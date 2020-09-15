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
		List<CustomSeries> AllSeries = new List<CustomSeries>();

		int MinRPM = int.MaxValue;
		int MaxRPM = int.MinValue;

		double MinTime = double.MaxValue;
		double MaxTime = double.MinValue;

		public GraphForm() {
			InitializeComponent();
			chart1.Series.Clear();
		}

		private void GraphForm_Load(object sender, EventArgs e) {
		}

		public void LoadGraph(LogData PowerRun, Color Clr, string Name) {
			foreach (LogEntry Entry in PowerRun.DataEntries) {
				if (Entry[PowerRun.RPM] < MinRPM)
					MinRPM = (int)Entry[PowerRun.RPM];
				if (Entry[PowerRun.RPM] > MaxRPM)
					MaxRPM = (int)Entry[PowerRun.RPM];

				if (Entry[PowerRun.DeviceTime] < MinTime)
					MinTime = Entry[PowerRun.DeviceTime];
				if (Entry[PowerRun.DeviceTime] > MaxTime)
					MaxTime = Entry[PowerRun.DeviceTime];
			}

			MinRPM = Utils.RoundToNearest(MinRPM, 500);
			MaxRPM = Utils.RoundToNearest(MaxRPM, 500, false);

			CustomSeries PowerSeries = CreateSeries(Name, MinRPM, MaxRPM, Clr, SeriesType.RPM);
			CustomSeries PowerRawSeries = CreateSeries(Name + "_raw", MinRPM, MaxRPM, Color.Yellow, SeriesType.RPM);
			CustomSeries TorqueSeries = null; //CreateSeries(Name + "_torque", MinRPM, MaxRPM, Clr, SeriesType.RPM);
			CustomSeries SpeedSeries = CreateSeries(Name + "_spd", MinTime, MaxTime, Clr, SeriesType.Time);

			//PrintGraph(PowerRun, series, series_spd, MinRPM, MaxRPM);
			PrintGraph2(PowerRun, PowerSeries, PowerRawSeries, TorqueSeries, SpeedSeries);

			EnableSeries(SeriesType.RPM);
		}

		CustomSeries CreateSeries(string Name, double MinX, double MaxX, Color Clr, SeriesType SType) {
			Series series = chart1.Series.Add(Name);
			series.ChartType = SeriesChartType.Line; //SeriesChartType.Spline;
			series.Color = Clr;
			series.BorderWidth = 1;
			//series.SetCustomProperty("LineTension", "0.1");


			//chart1.ChartAreas[0].AxisX.Minimum = MinX;
			//chart1.ChartAreas[0].AxisX.Maximum = MaxX;
			//chart1.ChartAreas[0].AxisX.Interval = 200;
			//chart1.ChartAreas[0].AxisY.Interval = 10;

			CustomSeries S = new CustomSeries(series, SType);
			AllSeries.Add(S);
			return S;
		}

		static void PrintGraph2(LogData PowerRun, CustomSeries PowerSeries, CustomSeries PowerRawSeries, CustomSeries TorqueSeries, CustomSeries SpeedSeries) {
			foreach (LogEntry E in PowerRun.DataEntries) {
				double Time = E[PowerRun.DeviceTime];
				double RPM = E[PowerRun.RPM];

				if (PowerSeries != null)
					PowerSeries.Series.Points.AddXY(RPM, E.Calculated.Power);

				if (PowerRawSeries != null)
					PowerRawSeries.Series.Points.AddXY(RPM, E.Calculated.PowerRaw);

				if (TorqueSeries != null)
					TorqueSeries.Series.Points.AddXY(RPM, E.Calculated.Torque);

				if (SpeedSeries != null)
					SpeedSeries.Series.Points.AddXY(Time, PowerRun.GetSpeed(E));
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
					Spd = (float)Utils.Lerp(Prev[PowerRun.RPM], PowerRun.GetSpeed(Prev), Next[PowerRun.RPM], PowerRun.GetSpeed(Next), RPM);
					HP = (float)Utils.Lerp(Prev[PowerRun.RPM], Prev.Calculated.Power, Next[PowerRun.RPM], Next.Calculated.Power, RPM);
				}

				//S.Points.AddXY(RPM, HP);
				RoadSpeed[i] = Spd;
				PowerHP[i] = HP;
				TorqueNM[i] = NM;
			}

			if (Smooth) {
				//Utils.NoiseReduction(ref PowerHP, 1);

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

		private void rPMGraphToolStripMenuItem_Click(object sender, EventArgs e) {
			RPMGraphBtn.Checked = true;
			TimeGraphBtn.Checked = false;
			EnableSeries(SeriesType.RPM);
		}

		private void timeGraphToolStripMenuItem_Click(object sender, EventArgs e) {
			TimeGraphBtn.Checked = true;
			RPMGraphBtn.Checked = false;
			EnableSeries(SeriesType.Time);
		}

		void EnableSeries(SeriesType SType) {
			foreach (CustomSeries S in AllSeries) {
				S.Series.Enabled = S.SeriesType == SType;
			}

			double XMin = 0;
			double XMax = 0;

			switch (SType) {
				case SeriesType.Time:
					XMin = MinTime;
					XMax = MaxTime;
					break;

				case SeriesType.RPM:
					XMin = MinRPM;
					XMax = MaxRPM;
					break;

				default:
					throw new NotImplementedException();
			}

			chart1.ChartAreas[0].AxisX.Minimum = XMin;
			chart1.ChartAreas[0].AxisX.Maximum = XMax;
			//chart1.ChartAreas[0].AxisX.Interval = 200;
			//chart1.ChartAreas[0].AxisY.Interval = 10;
			chart1.ResetAutoValues();
		}
	}

	class CustomSeries {
		public Series Series;
		public SeriesType SeriesType;

		public CustomSeries(Series Series, SeriesType SeriesType) {
			this.Series = Series;
			this.SeriesType = SeriesType;
		}
	}

	enum SeriesType {
		Time,
		RPM
	}
}
