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

		public void LoadGraph(IEnumerable<CSVEntry> PowerRun, Color Clr, string Name) {
			int MinRPM = 10000;
			int MaxRPM = 0;

			foreach (CSVEntry Entry in PowerRun) {
				if (Entry.RPM < MinRPM)
					MinRPM = (int)Entry.RPM;
				if (Entry.RPM > MaxRPM)
					MaxRPM = (int)Entry.RPM;

				//series.Points.AddXY(Entry.RPM, Entry.HP);
			}

			MinRPM = Utils.RoundToNearest(MinRPM, 500);
			MaxRPM = Utils.RoundToNearest(MaxRPM, 500, false);

			Series series = CreateSeries(Name, MinRPM, MaxRPM, Clr);
			//Series series2 = CreateSeries(Name + " nonsmooth", MinRPM, MaxRPM, Color.Blue);

			PrintGraph(PowerRun, series, MinRPM, MaxRPM);
			//PrintGraph(PowerRun, series2, MinRPM, MaxRPM, false);
		}

		Series CreateSeries(string Name, int MinRPM, int MaxRPM, Color Clr) {
			Series series = chart1.Series.Add(Name);
			series.ChartType = SeriesChartType.Spline;
			series.Color = Clr;
			series.BorderWidth = 2;

			chart1.ChartAreas[0].AxisX.Maximum = MaxRPM;
			chart1.ChartAreas[0].AxisX.Minimum = MinRPM;
			chart1.ChartAreas[0].AxisX.Interval = 200;
			chart1.ChartAreas[0].AxisY.Interval = 5;

			return series;
		}

		static void PrintGraph(IEnumerable<CSVEntry> PowerRun, Series S, int RPMStart, int RPMStop, bool Smooth = true, bool WHP = true) {
			//float ColumnRPMWidth = 500.0f / 6;

			int RPMRange = 100;
			int Count = (RPMStop - RPMStart) / RPMRange;

			float[] PowerHP = new float[Count];
			float[] TorqueNM = new float[Count];

			for (int i = 0; i < Count; i++) {
				int RPM = RPMStart + i * RPMRange;
				Program.FindNearest(PowerRun, RPM, out CSVEntry Prev, out CSVEntry Next);

				float HP = 0;
				float NM = 0;

				/*if (Prev == null && Next != null) {
					HP = Utils.Lerp(0, 0, Next.RPM, Next.HP, RPM);
					//NM = Utils.Lerp(0,0,Next.RPM,Next.)
				} else*/
				if (Prev != null && Next != null) {
					HP = Utils.Lerp(Prev.RPM, Prev.HP, Next.RPM, Next.HP, RPM);
				}

				//S.Points.AddXY(RPM, HP);
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
			}
		}
	}
}
