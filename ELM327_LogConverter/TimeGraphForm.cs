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
	public partial class TimeGraphForm : Form {
		public TimeGraphForm() {
			InitializeComponent();
			chart1.Series.Clear();
		}

		private void GraphForm_Load(object sender, EventArgs e) {
		}

		public void LoadGraph(IEnumerable<CSVEntry> PowerRun, Color Clr, string Name) {
			float MinTime = float.MaxValue;
			float MaxTime = float.MinValue;

			foreach (CSVEntry Entry in PowerRun) {
				if (Entry.DeviceTime < MinTime)
					MinTime = Entry.DeviceTime;

				if (Entry.DeviceTime > MaxTime)
					MaxTime = Entry.DeviceTime;

				//series.Points.AddXY(Entry.RPM, Entry.HP);
			}

			CSVEntry FirstEntry = PowerRun.First();
			int ItemCount = FirstEntry.Length;

			for (int i = 0; i < ItemCount; i++) {
				if (i == 2)
					continue;

				Series series = CreateSeries(Name + "_" + FirstEntry.GetIndexName(i), MinTime, MaxTime, Utils.RandomColor());
				PrintGraph(PowerRun, series, i);
			}
		}

		Series CreateSeries(string Name, float Min, float Max, Color Clr) {
			Series series = chart1.Series.Add(Name);
			series.ChartType = SeriesChartType.Line; //SeriesChartType.Spline;
			series.Color = Clr;
			series.BorderWidth = 2;

			chart1.ChartAreas[0].AxisX.Maximum = Max;
			chart1.ChartAreas[0].AxisX.Minimum = Min;
			chart1.ChartAreas[0].AxisX.Interval = 0.5;
			//chart1.ChartAreas[0].AxisY.Interval = 5;

			return series;
		}

		static void PrintGraph(IEnumerable<CSVEntry> PowerRun, Series S, int Idx) {
			foreach (CSVEntry Entry in PowerRun.OrderBy((E) => E.DeviceTime)) {
				S.Points.AddXY(Entry.DeviceTime, Entry[Idx]);
			}
		}
	}
}
