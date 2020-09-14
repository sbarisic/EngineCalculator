using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace ELM327_LogConverter {
	public partial class GraphForm : Form {
		public GraphForm() {
			InitializeComponent();
			chart1.Series.Clear();
		}

		private void GraphForm_Load(object sender, EventArgs e) {
		}

		public void LoadGraph(LogData PowerRun, Color Clr, string Name) {
			int MinRPM = 10000;
			int MaxRPM = 0;

			foreach (LogEntry Entry in PowerRun.DataEntries) {
				if (Entry[PowerRun.RPM] < MinRPM)
					MinRPM = (int)Entry[PowerRun.RPM];
				if (Entry[PowerRun.RPM] > MaxRPM)
					MaxRPM = (int)Entry[PowerRun.RPM];

				//series.Points.AddXY(Entry.RPM, Entry.HP);
			}

			MinRPM = Utils.RoundToNearest(MinRPM, 500);
			MaxRPM = Utils.RoundToNearest(MaxRPM, 500, false);

			Series series = CreateSeries(Name, MinRPM, MaxRPM, Clr);
			Series series_spd = null; // CreateSeries(Name + "_map", MinRPM, MaxRPM, Clr);

			//PrintGraph(PowerRun, series, series_spd, MinRPM, MaxRPM);
			PrintGraph2(PowerRun, series);
		}

		Series CreateSeries(string Name, int MinRPM, int MaxRPM, Color Clr) {
			Series series = chart1.Series.Add(Name);
			series.ChartType = SeriesChartType.Spline; //SeriesChartType.Spline;
			series.Color = Clr;
			series.BorderWidth = 2;

			chart1.ChartAreas[0].AxisX.Maximum = MaxRPM;
			chart1.ChartAreas[0].AxisX.Minimum = MinRPM;
			chart1.ChartAreas[0].AxisX.Interval = 200;
			chart1.ChartAreas[0].AxisY.Interval = 5;

			return series;
		}

		static void PrintGraph2(LogData PowerRun, Series S) {
			Dictionary<int, List<double>> RPMPower = new Dictionary<int, List<double>>();

			for (int i = 0; i < PowerRun.DataEntries.Length; i++) {
				int RPM = (int)PowerRun.DataEntries[i][PowerRun.RPM];
				double Power = PowerRun.DataEntries[i].Calculated.Power;

				if (!RPMPower.ContainsKey(RPM))
					RPMPower.Add(RPM, new List<double>());

				RPMPower[RPM].Add(Power);
			}

			Tuple<int, double>[] RPMPowerPairs = RPMPower.Select(KV => new Tuple<int, double>(KV.Key, KV.Value.Average())).OrderBy(KV => KV.Item1).ToArray();

			foreach (var KV in RPMPowerPairs) {
				S.Points.AddXY(KV.Item1, KV.Item2);
			}
		}

		static void PrintGraph(LogData PowerRun, Series S, Series S2, int RPMStart, int RPMStop, bool Smooth = false, bool WHP = true) {
			//float ColumnRPMWidth = 500.0f / 6;

			int RPMRange = 100;
			int Count = (RPMStop - RPMStart) / RPMRange;

			float[] RoadSpeed = new float[Count];
			float[] PowerHP = new float[Count];
			float[] TorqueNM = new float[Count];

			for (int i = 0; i < Count; i++) {
				int RPM = RPMStart + i * RPMRange;
				//Program.FindNearest(PowerRun, RPM, out LogEntry Prev, out LogEntry Next);

				PowerRun.FindNearest(RPM, out LogEntry Prev, out LogEntry Next);


				float Spd = 0;
				float HP = 0;
				float NM = 0;

				/*if (Prev == null && Next != null) {
					HP = Utils.Lerp(0, 0, Next.RPM, Next.HP, RPM);
					//NM = Utils.Lerp(0,0,Next.RPM,Next.)
				} else*/
				if (Prev != null && Next != null) {
					Spd = (float)Utils.Lerp(Prev[PowerRun.RPM], Prev[PowerRun.Speed], Next[PowerRun.RPM], Next[PowerRun.Speed], RPM);
					HP = (float)Utils.Lerp(Prev[PowerRun.RPM], Prev.Calculated.Power, Next[PowerRun.RPM], Next.Calculated.Power, RPM);
				}

				//S.Points.AddXY(RPM, HP);
				RoadSpeed[i] = Spd;
				PowerHP[i] = HP;
				TorqueNM[i] = NM;
			}

			if (Smooth) {
				Utils.NoiseReduction(ref PowerHP, 1);

				/*for (int i = 0; i < PowerHP.Length; i++)
					PowerHP[i] *= 1.05f;*/
			}

			float CompensationFactor = 1.2f;

			if (WHP)
				CompensationFactor = 1;

			for (int i = 0; i < Count; i++) {
				int RPM = RPMStart + i * RPMRange;

				S.Points.AddXY(RPM, PowerHP[i] * CompensationFactor);

				if (S2 != null)
					S2.Points.AddXY(RPM, RoadSpeed[i]);
			}
		}
	}
}
