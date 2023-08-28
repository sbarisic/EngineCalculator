using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Globalization;
using System.Windows.Forms;
using MathNet.Numerics;
using System.Drawing;

namespace ELM327_LogConverter {
	public delegate double ParseFunc(string Str);

	public class LogData {
		public LogIndex DeviceTime = new LogIndex(new string[] { "Device time", "Offset" }, LogIndex.ParseDate);
		public LogIndex RPM = new LogIndex(new string[] { "Engine RPM (rpm)", "Engine RPM (SAE)" }, LogIndex.ParseNum);
		LogIndex Speed = new LogIndex(new string[] { "Vehicle speed (km/h)", "Vehicle Speed (SAE)" }, LogIndex.ParseNum);
		LogIndex MAP = new LogIndex(new string[] { "Intake manifold absolute pressure (kPa)", "Intake Manifold Absolute Pressure (SAE)" }, LogIndex.ParseNum);

		public LogEntry[] DataEntries;
		//public CalculatedEntry[] CalculatedEntries;

		public int DataCount;

		FieldInfo[] LogIndexFields;
		LogIndex[] LogIndices;

		public string FileName;
		public string CalculatorFile;

		public int Gear = 2;
		public int Weight = 70;

		public LogData() {
			LogIndexFields = GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(F => F.FieldType == typeof(LogIndex)).ToArray();
			LogIndices = LogIndexFields.Select(F => (LogIndex)F.GetValue(this)).ToArray();
		}

		public LogData(string CSVFile) : this() {
			Parse(CSVFile);
		}

		void Parse(string CSVFile) {
			string CSVFileRaw = File.ReadAllText(CSVFile);

			if (CSVFileRaw.Contains("HP Tuners CSV Log File")) {
				DataEntries = ParseEntriesHPT(CSVFile).OrderBy(E => E[DeviceTime]).ToArray();
			} else {
				DataEntries = ParseEntries(CSVFile).OrderBy(E => E[DeviceTime]).ToArray();
			}

			// Make sure speed never decreases

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

			const double DistThr = 0.0;
			double MinDist = double.MaxValue;
			double MaxDist = double.MinValue;

			// Interpolate all data inbetween
			for (int i = 0; i < DataEntries.Length; i++) {
				if (i > 0 && i < DataEntries.Length - 1) {
					double CurTime = DataEntries[i][DeviceTime];
					double PrevTime = DataEntries[FindPrevious(i, DeviceTime)][DeviceTime];
					double NextTime = DataEntries[FindNext(i, DeviceTime)][DeviceTime];

					//double Dist = Math.Min(Utils.Distance(CurTime, PrevTime), Utils.Distance(CurTime, NextTime));
					double Dist = Utils.Distance(CurTime, PrevTime);

					if (Dist < DistThr) {
						DataEntries[i] = null;
						continue;
					}

					MinDist = Math.Min(MinDist, Dist);
					MaxDist = Math.Max(MaxDist, Dist);
				}


				foreach (var LogIdx in LogIndices) {
					if (LogIdx == DeviceTime)
						continue;

					if (DataEntries[i][LogIdx] == -1) {
						int PreviousIdx = FindPrevious(i, LogIdx);
						int NextIdx = FindNext(i, LogIdx);

						if (PreviousIdx >= i || NextIdx <= i)
							throw new Exception("Wat");

						LogEntry Previous = DataEntries[PreviousIdx];
						LogEntry Next = DataEntries[NextIdx];

						DataEntries[i][LogIdx] = Utils.Lerp(Previous[DeviceTime], Previous[LogIdx], Next[DeviceTime], Next[LogIdx], DataEntries[i][DeviceTime]);
					}
				}


			}

			// Remove NULL entries
			DataEntries = DataEntries.Where(E => E != null).ToArray();

			//Calculate();
		}

		public void Calculate() {
			// Find max and min RPM index
			int MaxRPMIdx = -1;
			int MinRPMIdx = -1;
			double MaxRPM = -1;
			double MinRPM = 999999;

			Calculator.Weight2 = Weight;

			for (int i = 0; i < DataEntries.Length; i++) {
				if (DataEntries[i][RPM] > MaxRPM) {
					MaxRPMIdx = i;
					MaxRPM = DataEntries[i][RPM];
				}
			}


			for (int i = 0; i < DataEntries.Length; i++) {
				if (DataEntries[i][RPM] < MinRPM) {
					MinRPMIdx = i;
					MinRPM = DataEntries[i][RPM];
				}
			}

			/*for (int i = MaxRPMIdx - 1; i >= 0; i--) {
				if (DataEntries[i][RPM] < DataEntries[i + 1][RPM]) {
					MinRPMIdx = i;
					MinRPM = DataEntries[i][RPM];
					continue;
				}

				break;
			}*/

			if (MaxRPMIdx == -1 || MinRPMIdx == -1 || (MaxRPM - MinRPMIdx) <= 1)
				throw new Exception("Invalid data");

			// Trim data
			DataEntries = DataEntries.Skip(MinRPMIdx).Take(MaxRPMIdx - MinRPMIdx).ToArray();

			// Normalize time
			double StartTime = DataEntries[0][DeviceTime];
			for (int i = 0; i < DataEntries.Length; i++)
				DataEntries[i][DeviceTime] = DataEntries[i][DeviceTime] - StartTime;

			// Smooth out speed
			/*double[] Speeds = DataEntries.Select(E => GetSpeed(E)).ToArray();
			Speeds = Utils.ApplyUKF(Speeds);
			for (int i = 0; i < DataEntries.Length; i++)
				SetSpeed(DataEntries[i], Speeds[i]);*/

			// Calculate all data
			const int CalcOffset = 1;
			double Dt = 0;
			DataEntries[0].Calculated = new CalculatedEntry();

			UKF Filter = new UKF(DataEntries.Length);

			double MaxPower = 0;
			double MaxTorque = 0;

			for (int i = CalcOffset; i < DataEntries.Length - CalcOffset; i++) {
				int PrevIdx = i - CalcOffset;
				int NextIdx = i + CalcOffset;

				double PrevTime = DataEntries[PrevIdx][DeviceTime];
				double CurTime = DataEntries[i][DeviceTime];
				double NextTime = DataEntries[NextIdx][DeviceTime];

				Dt = Utils.Distance(PrevTime, NextTime);

				double PrevSpeed = GetSpeed(PrevIdx);
				double CurSpeed = GetSpeed(i);
				double NextSpeed = GetSpeed(NextIdx);

				double PowerRaw = Calculator.Calculate(NextSpeed, PrevSpeed, NextTime, PrevTime);
				Filter.Update(new double[] { PowerRaw });

				//double Power = Filter.getState()[0];
				double Power = PowerRaw;

				double Torque = Calculator.CalcTorque(Power * 1.2, DataEntries[i][RPM]);

				if (Power > MaxPower)
					MaxPower = Power;

				if (Torque > MaxTorque)
					MaxTorque = Torque;

				DataEntries[i].Calculated = new CalculatedEntry(Power, PowerRaw, Torque);
			}

			Console.WriteLine("{0} whp; {1} Nm", MaxPower, MaxTorque);

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

			// Smooth out power raw
			double[] PowerRaws = DataEntries.Select(E => E.Calculated.PowerRaw).ToArray();
			Utils.NoiseReduction(ref PowerRaws, 1); // 4
			for (int i = 0; i < PowerRaws.Length; i++) {
				DataEntries[i].Calculated.Power = PowerRaws[i];
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
					if (DataEntries[i] != null)
						if (DataEntries[i][Idx] != -1)
							return i;
				} else {
					if (DataEntries[i] != null)
						if (DataEntries[i].Calculated != null)
							return i;
				}
			}

			return -1;
		}

		int FindPrevious(int StartIdx, LogIndex Idx) {
			for (int i = StartIdx - 1; i >= 0; i--) {
				if (Idx != null) {
					if (DataEntries[i] != null)
						if (DataEntries[i][Idx] != -1)
							return i;
				} else {
					if (DataEntries[i] != null)
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

			return Calculator.CalcSpeed(Gear, Entry[RPM]);
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

				if (Idx != null && Idx.MatchesName(Name))
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

		int FindIndex(string[] Lines, string Src) {
			for (int i = 0; i < Lines.Length; i++) {
				if (Lines[i] == Src)
					return i;
			}

			return 0;
		}

		IEnumerable<LogEntry> ParseEntriesHPT(string CSVFile) {
			string[] Lines = File.ReadAllText(CSVFile).Trim().Split(new[] { '\n' }).Select(L => L.Trim()).ToArray();
			DataCount = 0;

			int ChannelInfoIdx = FindIndex(Lines, "[Channel Information]");
			int ChannelDataIdx = FindIndex(Lines, "[Channel Data]");

			string[] ChannelInfo = Lines.Skip(ChannelInfoIdx + 1).Take(ChannelDataIdx - ChannelInfoIdx - 1).ToArray();
			string[] InfoNames = ChannelInfo[1].Split(new[] { ',' }).ToArray();

			for (int i = 0; i < InfoNames.Length; i++) {
				LogIndex Idx = GetField(InfoNames[i]);

				if (Idx != null) {
					Idx.CsvIndex = i;
					Idx.Index = DataCount++;
				}
			}

			LogIndices = LogIndices.Where(LI => LI.IsValid()).ToArray();
			string[][] DataLines = Lines.Skip(ChannelDataIdx + 1).Select(L => L.Split(',')).ToArray();
			LogEntry LastEntry = null;

			for (int i = 0; i < DataLines.Length; i++) {
				LogEntry Entry = new LogEntry(DataCount);

				foreach (LogIndex LogIndex in LogIndices) {
					/*if (!LogIndex.IsValid())
						continue;*/

					Entry[LogIndex] = LogIndex.Parse(DataLines[i][LogIndex.CsvIndex]);
				}

				if (LastEntry != null && LastEntry[RPM.Index] > Entry[RPM.Index]) {
					continue;
				}

				LastEntry = Entry;
				yield return Entry;
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

		public string Serialize() {
			StringBuilder Serialized = new StringBuilder();

			foreach (LogIndex Idx in LogIndices)
				Serialized.AppendLine(Idx.Serialize());

			Serialized.AppendLine("#gear " + Gear);
			Serialized.AppendLine("#weight " + Weight);

			foreach (LogEntry Entry in DataEntries)
				Serialized.AppendLine(Entry.Serialize());

			return Serialized.ToString();
		}

		public void Deserialize(string SourceFile) {
			FileName = Path.GetFileNameWithoutExtension(SourceFile);
			string[] Source = File.ReadAllLines(SourceFile);

			List<LogEntry> DataEntriesList = new List<LogEntry>();

			for (int i = 0; i < Source.Length; i++) {
				string SourceLine = Source[i];

				if (SourceLine.StartsWith("#!")) {
					LogIndex.Deserialize(SourceLine, out int Index, out string Name);
					LogIndex Idx = LogIndices.Where(I => I.Name == Name).FirstOrDefault();
					Idx.Index = Index;
				} else if (SourceLine.StartsWith("#?")) {

				} else if (SourceLine.StartsWith("#gear")) {
					Gear = int.Parse(SourceLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1]);
				} else if (SourceLine.StartsWith("#weight")) {
					Weight = int.Parse(SourceLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1]);
				} else {
					LogEntry Entry = new LogEntry();
					Entry.Deserialize(SourceLine);
					DataEntriesList.Add(Entry);
				}
			}

			DataEntries = DataEntriesList.ToArray();
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
			PowerRaw = Utils.Lerp(Prev[Data.DeviceTime], Prev.Calculated.PowerRaw, Next[Data.DeviceTime], Next.Calculated.PowerRaw, Time);
			Torque = Utils.Lerp(Prev[Data.DeviceTime], Prev.Calculated.Torque, Next[Data.DeviceTime], Next.Calculated.Torque, Time);
		}

		public override string ToString() {
			return Utils.ToString(Power) + ", " + Utils.ToString(PowerRaw) + ", " + Utils.ToString(Torque);
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

		public LogEntry() {
		}

		public LogEntry(int Len) {
			Data = new double[Len];
		}

		public string Serialize() {
			return string.Join(";", Data.Select(Utils.Serialize));
		}

		public void Deserialize(string Line) {
			Data = Line.Split(';').Select(Utils.DeserializeDouble).ToArray();
		}

		public override string ToString() {
			string Str = string.Join(", ", Data.Select(D => Utils.ToString(D)));
			Str = string.Format("{0} ; {1}", Str, Calculated?.ToString() ?? "null");
			return Str;
		}
	}

	public class LogIndex {
		public string Name;
		public string[] AltNames;

		public int CsvIndex;
		public int Index;
		public ParseFunc Parse;

		public LogIndex(string[] Names, ParseFunc Parse) {
			CsvIndex = -1;
			Index = -1;

			this.Name = Names[0];
			AltNames = Names.Skip(1).ToArray();

			this.Parse = Parse;
		}

		public bool IsValid() {
			if (Index == -1)
				return false;

			return true;
		}

		public bool MatchesName(string Name) {
			if (this.Name == Name)
				return true;

			if (AltNames != null) {
				for (int i = 0; i < AltNames.Length; i++) {
					if (AltNames[i] == Name)
						return true;
				}
			}

			return false;
		}

		public string Serialize() {
			return string.Format("#! {0} {1}", Index, Name);
		}

		public static void Deserialize(string Line, out int Index, out string Name) {
			Line = Line.Substring(3, Line.Length - 3);
			int IdxOfSpace = Line.IndexOf(' ');
			string IndexStr = Line.Substring(0, IdxOfSpace);

			Index = int.Parse(IndexStr);
			Name = Line.Substring(IdxOfSpace + 1, Line.Length - IdxOfSpace - 1);
		}

		public static double ParseDate(string Str) {
			if (Str.Count(C => C == '.') == 1)
				return double.Parse(Str, CultureInfo.InvariantCulture);

			return DateTime.Parse(Str).TimeOfDay.TotalSeconds;
		}

		public static double ParseNum(string Str) {
			if (Str.Length == 0)
				return -1;

			return float.Parse(Str, CultureInfo.InvariantCulture);
		}
	}
}
