using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Globalization;
using System.Windows.Forms;

namespace ELM327_LogConverter {
	public delegate double ParseFunc(string Str);

	public class LogData {
		public LogIndex DeviceTime = new LogIndex("Device time", LogIndex.ParseDate);
		public LogIndex RPM = new LogIndex("Engine RPM (rpm)", LogIndex.ParseNum);
		LogIndex Speed = new LogIndex("Vehicle speed (km/h)", LogIndex.ParseNum);
		LogIndex MAP = new LogIndex("Intake manifold absolute pressure (kPa)", LogIndex.ParseNum);

		public LogEntry[] DataEntries;
		//public CalculatedEntry[] CalculatedEntries;

		public int DataCount;

		FieldInfo[] LogIndexFields;
		LogIndex[] LogIndices;

		public LogData() {
			LogIndexFields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance).Where(F => F.FieldType == typeof(LogIndex)).ToArray();
			LogIndices = LogIndexFields.Select(F => (LogIndex)F.GetValue(this)).ToArray();
		}

		public LogData(string CSVFile) : this() {
			Parse(CSVFile);
		}

		void Parse(string CSVFile) {
			DataEntries = ParseEntries(CSVFile).OrderBy(E => E[DeviceTime]).ToArray();

			// Fix first entry and last entry to contain all valid data
			foreach (var Idx in LogIndices) {
				if (DataEntries[0][Idx] == -1) {
					int FoundIdx = FindNext(0, Idx);
					DataEntries[0][Idx] = DataEntries[FoundIdx][Idx];
				}

				if (DataEntries[DataEntries.Length - 1][Idx] == -1) {
					int FoundIdx = FindPrevious(DataEntries.Length - 1, Idx);
					DataEntries[DataEntries.Length - 1][Idx] = DataEntries[FoundIdx][Idx];
				}
			}

			int MaxRPMIdx = -1;
			int MinRPMIdx = -1;
			double MaxRPM = -1;

			// Interpolate all data inbetween, find max RPM index
			for (int i = 0; i < DataEntries.Length; i++) {
				foreach (var LogIdx in LogIndices) {
					if (LogIdx == DeviceTime)
						continue;

					if (DataEntries[i][LogIdx] == -1) {
						LogEntry Previous = DataEntries[FindPrevious(i, LogIdx)];
						LogEntry Next = DataEntries[FindNext(i, LogIdx)];

						DataEntries[i][LogIdx] = Utils.Lerp(Previous[DeviceTime], Previous[LogIdx], Next[DeviceTime], Next[LogIdx], DataEntries[i][DeviceTime]);
					}
				}

				if (DataEntries[i][RPM] > MaxRPM) {
					MaxRPMIdx = i;
					MaxRPM = DataEntries[i][RPM];
				}
			}

			// Find min RPM index
			for (int i = MaxRPMIdx - 1; i >= 0; i--) {
				if (DataEntries[i][RPM] < DataEntries[i + 1][RPM]) {
					MinRPMIdx = i;
					continue;
				}

				break;
			}

			if (MaxRPMIdx == -1 || MinRPMIdx == -1 || (MaxRPM - MinRPMIdx) <= 1)
				throw new Exception("Invalid data");

			// Trim data
			DataEntries = DataEntries.Skip(MinRPMIdx).Take(MaxRPMIdx - MinRPMIdx).ToArray();

			// Normalize time
			double StartTime = DataEntries[0][DeviceTime];
			for (int i = 0; i < DataEntries.Length; i++)
				DataEntries[i][DeviceTime] = DataEntries[i][DeviceTime] - StartTime;

			/*// Smooth out speed
			double[] Speeds = DataEntries.Select(E => GetSpeed(E)).ToArray();
			for (int i = 0; i < DataEntries.Length; i++)
				SetSpeed(DataEntries[i], Speeds[i]);*/

			// Calculate all data
			const double CalcInterval = 0.5;
			int PrevIdx = 0;
			double Dt = 0;
			DataEntries[0].Calculated = new CalculatedEntry();

			UKF Filter = new UKF();

			for (int i = 1; i < DataEntries.Length; i++) {
				Dt = DataEntries[i][DeviceTime] - DataEntries[PrevIdx][DeviceTime];
				if (Dt < CalcInterval)
					continue;

				double PrevTime = DataEntries[PrevIdx][DeviceTime];
				double CurTime = DataEntries[i][DeviceTime];

				double PrevSpeed = GetSpeed(PrevIdx);
				double CurSpeed = GetSpeed(i);

				double PowerRaw = Calculator.Calculate(CurSpeed, PrevSpeed, CurTime, PrevTime);
				Filter.Update(new double[] { PowerRaw });

				double Power = Filter.getState()[0];
				double Torque = Calculator.CalcTorque(Power, DataEntries[i][RPM]);

				DataEntries[i].Calculated = new CalculatedEntry(Power, PowerRaw, Torque);

				PrevIdx = i;
			}

			// Fill last index
			int LastIdx = DataEntries.Length - 1;
			if (DataEntries[LastIdx].Calculated == null) {
				int Prev = FindPrevious(LastIdx, null);
				DataEntries[LastIdx].Calculated = new CalculatedEntry(DataEntries[Prev].Calculated);
			}

			// Interpolate calculated data
			for (int i = 0; i < DataEntries.Length; i++) {
				if (DataEntries[i].Calculated == null) {
					LogEntry Prev = DataEntries[FindPrevious(i, null)];
					LogEntry Next = DataEntries[FindNext(i, null)];

					DataEntries[i].Calculated = new CalculatedEntry(this, Prev, Next, DataEntries[i][DeviceTime]);
				}
			}
		}

		public void FindNearest(double RPM, out LogEntry Prev, out LogEntry Next) {
			Prev = DataEntries.Where(D => (D[this.RPM] - RPM) < 0).LastOrDefault();

			int Skip = 1;

			if (Prev == null)
				Skip = 0;

			Next = DataEntries.Where(D => (D[this.RPM] - RPM) > 0).Skip(Skip).FirstOrDefault();
		}

		int FindNext(int StartIdx, LogIndex Idx) {
			for (int i = StartIdx + 1; i < DataEntries.Length; i++) {
				if (Idx != null) {
					if (DataEntries[i][Idx] != -1)
						return i;
				} else {
					if (DataEntries[i].Calculated != null)
						return i;
				}
			}

			return -1;
		}

		int FindPrevious(int StartIdx, LogIndex Idx) {
			for (int i = StartIdx - 1; i >= 0; i--) {
				if (Idx != null) {
					if (DataEntries[i][Idx] != -1)
						return i;
				} else {
					if (DataEntries[i].Calculated != null)
						return i;
				}
			}

			return -1;
		}

		public double GetSpeed(int Idx) {
			return GetSpeed(DataEntries[Idx]);
		}

		public double GetSpeed(LogEntry Entry) {
			if (Speed.IsValid())
				return Entry[Speed];

			return Calculator.CalcSpeed(2, Entry[RPM]);
		}

		public void SetSpeed(LogEntry Entry, double Spd) {
			if (Speed.IsValid()) {
				Entry[Speed] = Spd;
				return;
			}

			Entry[RPM] = Calculator.CalcRPM(2, Spd);
		}

		LogIndex GetField(string Name) {
			for (int i = 0; i < LogIndexFields.Length; i++) {
				LogIndex Idx = (LogIndex)LogIndexFields[i].GetValue(this);

				if (Idx?.Name == Name)
					return Idx;
			}

			return null;
		}

		IEnumerable<LogEntry> ParseEntries(string CSVFile) {
			string[] Lines = File.ReadAllLines(CSVFile);
			DataCount = 0;

			for (int i = 0; i < Lines.Length; i++) {
				string[] Values = Lines[i].Split(new[] { ',' }).Select(Utils.StripQuotes).ToArray();

				if (i == 0) {
					for (int j = 0; j < Values.Length; j++) {
						LogIndex Idx = GetField(Values[j]);

						if (Idx != null) {
							Idx.CsvIndex = j;
							Idx.Index = DataCount++;
						}
					}

					LogIndices = LogIndices.Where(LI => LI.IsValid()).ToArray();
				} else {
					LogEntry Entry = new LogEntry(DataCount);

					foreach (LogIndex LogIndex in LogIndices) {
						/*if (!LogIndex.IsValid())
							continue;*/

						Entry[LogIndex] = LogIndex.Parse(Values[LogIndex.CsvIndex]);
					}

					yield return Entry;
				}
			}
		}

		public string GenerateCSV() {
			StringBuilder SB = new StringBuilder();
			SB.AppendLine(string.Join(";", LogIndices.OrderBy(LI => LI.Index).Select(LI => LI.Name).ToArray()));

			foreach (LogEntry Entry in DataEntries) {
				SB.AppendLine(Entry.ToString());
			}

			return SB.ToString();
		}
	}

	public class CalculatedEntry {
		public double Power;
		public double PowerRaw;
		public double Torque;

		public CalculatedEntry() {
			Power = 0;
			PowerRaw = 0;
			Torque = 0;
		}

		public CalculatedEntry(double Power, double PowerRaw, double Torque) {
			this.Power = Power;
			this.PowerRaw = PowerRaw;
			this.Torque = Torque;
		}

		public CalculatedEntry(CalculatedEntry Copy) {
			Power = Copy.Power;
			Torque = Copy.Torque;
		}

		public CalculatedEntry(LogData Data, LogEntry Prev, LogEntry Next, double Time) {
			Power = Utils.Lerp(Prev[Data.DeviceTime], Prev.Calculated.Power, Next[Data.DeviceTime], Next.Calculated.Power, Time);
		}

		public override string ToString() {
			return Utils.ToString(Power) + ", " + Utils.ToString(Torque);
		}
	}

	public class LogEntry {
		public CalculatedEntry Calculated;

		double[] Data;

		public double this[LogIndex Idx] {
			get {
				return this[Idx.Index];
			}

			set {
				this[Idx.Index] = value;
			}
		}

		public double this[int Idx] {
			get {
				return Data[Idx];
			}

			set {
				Data[Idx] = value;
			}
		}

		public LogEntry(int Len) {
			Data = new double[Len];
		}

		public override string ToString() {
			return string.Join(", ", Data.Select(D => Utils.ToString(D)));
		}
	}

	public class LogIndex {
		public string Name;
		public int CsvIndex;
		public int Index;
		public ParseFunc Parse;

		public LogIndex(string Name, ParseFunc Parse) {
			CsvIndex = -1;
			Index = -1;

			this.Name = Name;
			this.Parse = Parse;
		}

		public bool IsValid() {
			if (Index == -1)
				return false;

			return true;
		}

		public static double ParseDate(string Str) {
			return DateTime.Parse(Str).TimeOfDay.TotalSeconds;
		}

		public static double ParseNum(string Str) {
			if (Str.Length == 0)
				return -1;

			return float.Parse(Str, CultureInfo.InvariantCulture);
		}
	}
}
