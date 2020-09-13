using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Globalization;

namespace ELM327_LogConverter {
	public delegate double ParseFunc(string Str);

	public class LogData {
		public LogIndex DeviceTime = new LogIndex("Device time", LogIndex.ParseDate);
		public LogIndex RPM = new LogIndex("Engine RPM (rpm)", LogIndex.ParseNum);
		public LogIndex Speed = new LogIndex("Vehicle speed (km/h)", LogIndex.ParseNum);
		public LogIndex MAP = new LogIndex("Intake manifold absolute pressure (kPa)", LogIndex.ParseNum);

		public LogEntry[] DataEntries;
		//public CalculatedEntry[] CalculatedEntries;

		public int DataCount;

		FieldInfo[] LogIndexFields;
		LogIndex[] LogIndices;

		public LogData() {
			LogIndexFields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance).Where(F => F.FieldType == typeof(LogIndex)).ToArray();
			LogIndices = LogIndexFields.Select(F => (LogIndex)F.GetValue(this)).ToArray();
		}

		public void Parse(string CSVFile) {
			DataEntries = ParseEntries(CSVFile).OrderBy(E => E[DeviceTime]).ToArray();
			double StartTime = DataEntries[0][DeviceTime];

			for (int i = 0; i < DataEntries.Length; i++)
				DataEntries[i][DeviceTime] = DataEntries[i][DeviceTime] - StartTime;

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

			// Interpolate all data inbetween
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
			}

			//CalculatedEntries = new CalculatedEntry[DataEntries.Length];
			//CalculatedEntries[0] = new CalculatedEntry(0);

			int PrevIdx = 0;
			//double Dt = 0;
			DataEntries[0].Calculated = new CalculatedEntry(0);

			for (int i = 1; i < DataEntries.Length; i++) {
				/*Dt = DataEntries[i][DeviceTime] - DataEntries[PrevIdx][DeviceTime];
				if (Dt < 1)
					continue;*/

				double CurTime = DataEntries[i][DeviceTime];
				double PrevTime = DataEntries[PrevIdx][DeviceTime];

				double Power = Calculator.Calculate(GetSpeed(i), GetSpeed(PrevIdx), CurTime, PrevTime);
				DataEntries[i].Calculated = new CalculatedEntry(Power);

				PrevIdx = i;
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
				if (DataEntries[i][Idx] != -1)
					return i;
			}

			return -1;
		}

		int FindPrevious(int StartIdx, LogIndex Idx) {
			for (int i = StartIdx - 1; i >= 0; i--) {
				if (DataEntries[i][Idx] != -1)
					return i;
			}

			return -1;
		}

		double GetSpeed(int Idx) {
			return DataEntries[Idx][Speed];
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
				} else {
					LogEntry Entry = new LogEntry(DataCount);

					foreach (var LogIndex in LogIndices) {
						Entry[LogIndex] = LogIndex.Parse(Values[LogIndex.CsvIndex]);
					}

					yield return Entry;
				}
			}
		}


	}

	public class CalculatedEntry {
		public double Power;
		public double Torque;

		public CalculatedEntry(double Power) {
			this.Power = Power;
		}

		public override string ToString() {
			return Utils.ToString(Power);
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
