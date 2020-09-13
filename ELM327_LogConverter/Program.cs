using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Security.Cryptography;

namespace ELM327_LogConverter {
	class Program {
		public static int AFR_Idx = -1;
		public static int TPS_Idx = -1;
		public static int RPM_Idx = -1;
		public static int MAP_Idx = -1;
		public static int SPD_Idx = -1;

		static void Main(string[] args) {
			Calculator.LoadCarData(File.ReadAllText("car_data.cfg"));

			Main2();
			return;




			string InputName = "input.csv";
			//string InputName = "2020-09-05 19-04-08.csv";

			if (args.Length != 0)
				InputName = args[0];

			GraphForm Frm = new GraphForm();
			//TimeGraphForm Frm = new TimeGraphForm();

			//Frm.LoadGraph(LoadRun("input.csv", false), Color.Blue, "Stock");
			//Frm.LoadGraph(LoadRun("2020-09-05 19-04-08.csv", true), Color.Red, "Remap");
			//Frm.LoadGraph(LoadRun("2020-09-12 19-48-04-fix.csv", true), Color.Green, "Remap 2");

			Application.Run(Frm);
		}

		static void Main2() {
			LogData Log = new LogData();
			Log.Parse("2020-09-12 19-48-04-fix.csv");

			GraphForm Frm = new GraphForm();
			Frm.LoadGraph(Log, Color.Red, "Remap 2");

			Application.Run(Frm);
		}

		/*static void LoadAndDisplayRun(GraphForm Frm, string InputName, string RunName, Color Clr) {
			IEnumerable<CSVEntry> PowerRunNew = LoadRun(InputName, false);
			Frm.LoadGraph(PowerRunNew, Clr, RunName);
		}*/

		static IEnumerable<CSVEntry> LoadRun(string InputName, bool UseNewMethod) {
			string[] InputLines = File.ReadAllLines(InputName);
			List<CSVEntry> ParsedEntries = new List<CSVEntry>();

			for (int i = 0; i < InputLines.Length; i++) {

				if (i == 0) {
					string[] Names = InputLines[i].Split(new[] { ',' }).Select(StripQuotes).ToArray();

					for (int j = 0; j < Names.Length; j++) {
						switch (Names[j]) {
							case "Fuel/Air commanded equivalence ratio ()":
								AFR_Idx = j;
								break;

							case "Throttle position (%)":
								TPS_Idx = j;
								break;

							case "Engine RPM (rpm)":
								RPM_Idx = j;
								break;

							case "Vehicle speed (km/h)":
								SPD_Idx = j;
								break;

							case "Intake manifold absolute pressure (kPa)":
								MAP_Idx = j;
								break;
						}
					}

				} else {
					CSVEntry Ent = new CSVEntry(InputLines[i]);

					if (Ent.IsValid())
						ParsedEntries.Add(Ent);
				}
			}

			ParsedEntries.OrderBy(E => E.Time);
			float MinTime = ParsedEntries.Min(E => E.DeviceTime);
			float MaxThrottle = ParsedEntries.Max(E => E.TPS);

			while (RunFixup(ParsedEntries))
				;

			//float FROM = 140.42f;
			//float TO = 152.46f;

			float FROM = 0;
			float TO = 0;

			for (int i = 0; i < ParsedEntries.Count; i++) {
				ParsedEntries[i].TPS = ParsedEntries[i].TPS * (100.0f / MaxThrottle);

				if (!ParsedEntries[i].FixupTime(MinTime, FROM, TO))
					ParsedEntries[i].MarkInvalid();

				if (i >= 1) {
					if (ParsedEntries[i].RPM == ParsedEntries[i - 1].RPM)
						ParsedEntries[i].MarkInvalid();
				}
			}

			for (int i = ParsedEntries.Count - 2; i >= 0; i--) {
				if (ParsedEntries[i].TPS >= 99 && ParsedEntries[i + 1].TPS < 99)
					ParsedEntries[i].TPS += 10;
			}

			ParsedEntries = new List<CSVEntry>(ParsedEntries.Where(E => E.IsValid()));


			for (int i = 1; i < ParsedEntries.Count; i++) {
				float CurRPM = ParsedEntries[i].RPM;
				float PrevRPM = ParsedEntries[i - 1].RPM;

				float CurTime = ParsedEntries[i].DeviceTime;
				float PrevTime = ParsedEntries[i - 1].DeviceTime;

				float CurSpeed = ParsedEntries[i].SPD;
				float PrevSpeed = ParsedEntries[i - 1].SPD;

				float HP = 0;

				if (UseNewMethod)
					HP = Calculator.Calculate(CurSpeed, PrevSpeed, CurTime, PrevTime);
				else
					HP = Calculator.Calculate(2, CurRPM, PrevRPM, CurTime, PrevTime, out CurSpeed);

				ParsedEntries[i].HP = HP;
				ParsedEntries[i].SPD = CurSpeed;
			}

			if (File.Exists("out.csv"))
				File.Delete("out.csv");

			string OutText = CSVEntry.GetFormat() + "\n" + string.Join("\n", ParsedEntries.Select(E => E.ToString()));
			File.WriteAllBytes("out.csv", Encoding.UTF8.GetBytes(OutText));

			Console.WriteLine(CSVEntry.GetFormat());
			IEnumerable<CSVEntry> PowerRun = ParsedEntries.Where(E => E.TPS >= 99 && E.TPS <= 101);
			Console.WriteLine(string.Join("\n", PowerRun.Select(E => string.Format("{0} => {1} whp", E.ToString(), E.HP)).ToArray()));

			CSVEntry MaxHP = PowerRun.OrderByDescending(Run => Run.HP).First();
			Console.WriteLine("Max {0} whp @ {1} RPM", MaxHP.HP, MaxHP.RPM);

			return PowerRun;
		}

		static void PrintGraph(IEnumerable<CSVEntry> PowerRun) {
			float ColumnRPMWidth = 500.0f / 6;

			for (int i = 0; i < 12; i++) {
				int RPM = 1000 + i * 500;

				FindNearest(PowerRun, RPM, out CSVEntry Prev, out CSVEntry Next);

				Console.CursorLeft = 6 * i;
				Console.CursorTop = Console.WindowHeight - 1;
				Console.Write(RPM);

				for (int j = 0; j < 6; j++) {
					int CurRPM = (int)((RPM - ColumnRPMWidth * 2) + ColumnRPMWidth);
					float HP = 0;

					if (Prev == null && Next != null)
						HP = (float)Utils.Lerp(0, 0, Next.RPM, Next.HP, CurRPM);
					else if (Prev != null && Next != null)
						HP = (float)Utils.Lerp(Prev.RPM, Prev.HP, Next.RPM, Next.HP, CurRPM);

					int ColumnHeight = (int)(HP / 10);

					DrawColumn(6 * i + j, Console.WindowHeight - 2, ColumnHeight);
				}
			}
		}

		static void DrawColumn(int X, int Y, int Height) {

			for (int i = 0; i < Height; i++) {
				Console.CursorLeft = X;
				Console.CursorTop = Y - i;
				Console.Write("#");
			}
		}

		public static void FindNearest(IEnumerable<CSVEntry> PowerRun, int RPM, out CSVEntry Prev, out CSVEntry Next) {
			Prev = PowerRun.Where(D => (D.RPM - RPM) < 0).LastOrDefault();

			/*if (Prev.RPM > RPM)
				Prev = null;*/

			int Skip = 1;

			if (Prev == null)
				Skip = 0;

			Next = PowerRun.Where(D => (D.RPM - RPM) > 0).Skip(Skip).FirstOrDefault();
		}

		static bool RunFixup(List<CSVEntry> Entries) {
			bool DidFixup = false;

			for (int i = 0; i < Entries.Count; i++) {

				for (int j = 0; j < Entries[i].Length; j++) {

					if (Entries[i][j] == -1) {
						if (i + 1 < Entries.Count && Entries[i + 1][j] != -1) {
							DidFixup = true;
							Entries[i][j] = Entries[i + 1][j];
						} else if (i - 1 >= 0 && Entries[i - 1][j] != -1) {
							DidFixup = true;
							Entries[i][j] = Entries[i - 1][j];
						}
					}
				}



			}

			return DidFixup;
		}

		public static string StripQuotes(string Line) {
			if (Line.StartsWith("\"") && Line.EndsWith("\""))
				return Line.Substring(1, Line.Length - 2);

			return Line;
		}
	}

	public class CSVEntry {
		public DateTime Time;
		public string[] Entries;

		public float DeviceTime;
		public float AFR;
		public float TPS;
		public float RPM;
		public float MAP;
		public float SPD;

		public float HP;
		bool MarkedInvalid = false;

		static float Stoich = 14.64f;

		public int Length {
			get {
				return 5;
			}
		}

		public float this[int idx] {
			get {
				switch (idx) {
					case 0:
						return AFR;

					case 1:
						return TPS;

					case 2:
						return RPM;

					case 3:
						return MAP;

					case 4:
						return SPD;

					default:
						throw new NotImplementedException();
				}
			}

			set {
				switch (idx) {
					case 0:
						AFR = value;
						break;

					case 1:
						TPS = value;
						break;

					case 2:
						RPM = value;
						break;

					case 3:
						MAP = value;
						break;

					case 4:
						SPD = value;
						break;

					default:
						throw new NotImplementedException();
				}
			}
		}

		public string GetIndexName(int Idx) {
			switch (Idx) {
				case 0:
					return "AFR";

				case 1:
					return "TPS";

				case 2:
					return "RPM";

				case 3:
					return "MAP";

				case 4:
					return "Speed";

				default:
					return "Unknown";
			}
		}

		public CSVEntry(string Line) {
			Entries = Line.Split(new[] { ',' }).Select(Program.StripQuotes).ToArray();
			Time = DateTime.Parse(Entries[0]);

			DeviceTime = (float)Time.TimeOfDay.TotalSeconds;

			AFR = TryParse(Entries, Program.AFR_Idx);
			TPS = TryParse(Entries, Program.TPS_Idx);
			RPM = (int)TryParse(Entries, Program.RPM_Idx);
			MAP = (int)TryParse(Entries, Program.MAP_Idx);
			SPD = TryParse(Entries, Program.SPD_Idx);

			/*if (MAP != -1)
				Debugger.Break();*/

			if (AFR != -1)
				AFR = AFR * Stoich;
		}


		public bool FixupTime(float T, float FromTime, float ToTime) {
			DeviceTime = DeviceTime - T + 1;
			/*DeviceTime = DeviceTime * 10;
			DeviceTime = (int)DeviceTime;*/

			if (FromTime != 0 && ToTime != 0)
				if (DeviceTime < FromTime || DeviceTime > ToTime)
					return false;

			DeviceTime -= FromTime;
			return true;
		}

		public void MarkInvalid() {
			MarkedInvalid = true;
		}

		public bool IsValid() {
			if (MarkedInvalid)
				return false;

			for (int i = 0; i < Length; i++)
				if (this[i] != -1)
					return true;

			return false;
		}

		float TryParse(string[] Entries, int Idx) {
			if (Idx < 0)
				return -1;

			if (Entries.Length <= Idx)
				return -1;

			string Str = Entries[Idx];

			if (string.IsNullOrEmpty(Str))
				return -1;

			return float.Parse(Str, CultureInfo.InvariantCulture);
		}

		public override string ToString() {
			//return string.Join(",", ToStringe(DeviceTime, 3), ToStringe(AFR, 3), ToStringe(TPS, 0), ToStringe(RPM, 0));
			return string.Join(", ", ToStringe(DeviceTime, 2), /*ToStringe(AFR, 2),*/ ToStringe(TPS, 2), ToStringe(RPM, 0), ToStringe((MAP / 100) - 1, 2));
		}

		static string ToStringe(float F, int Places) {
			if (F == -1)
				return "";

			if (Places == -1)
				return string.Format(CultureInfo.InvariantCulture, "{0}", F);

			if (Places == 0)
				return string.Format(CultureInfo.InvariantCulture, "{0:0}", F);

			return string.Format(CultureInfo.InvariantCulture, "{0:0." + new string('0', Places) + "}", F);
		}

		public static string GetFormat() {
			//return "\"Device time\",\"AFR\",\"Throttle position (%)\",\"Engine RPM (rpm)\"";
			//return "\"Device time\",\"AFR\",\"Throttle position (%)\",\"Engine RPM (rpm)\",\"Boost\"";

			//return "Time (ms), AFR, Throttle position (%), Engine RPM (rpm), Boost";
			return "Time (sec), Throttle position (%), Engine RPM (RPM), MAP (Bar)";
		}
	}
}
