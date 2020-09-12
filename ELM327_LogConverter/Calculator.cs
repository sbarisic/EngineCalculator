using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Windows.Forms;
//using VirtualDyno.Core;

namespace ELM327_LogConverter {
	static class Calculator {
		public static string Car;

		// lbs
		public static float Weight;
		public static float Weight2;

		public static float[] Gear = new float[6];

		public static float Final;
		public static float DragCoef;
		public static float FrontalArea;

		// inch
		public static float TireDiam;

		public static void LoadCarData(string Src) {
			string[] Lines = Src.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

			for (int i = 0; i < Lines.Length; i++) {
				string[] Line = Lines[i].Trim().Split(new[] { ' ' });

				string ValueRaw = Lines[i].Substring(Line[0].Length).Trim();
				string Value = Line[1];


				switch (Line[0]) {
					case "car":
						Car = ValueRaw;
						break;

					case "weight":
						// Weight = float.Parse(Value, CultureInfo.InvariantCulture) * 2.20462f;
						Weight = float.Parse(Value, CultureInfo.InvariantCulture);

						break;

					case "weight2":
						// Weight2 = float.Parse(Value, CultureInfo.InvariantCulture) * 2.20462f;
						Weight2 = float.Parse(Value, CultureInfo.InvariantCulture);
						break;

					case "gear1":
						Gear[0] = float.Parse(Value, CultureInfo.InvariantCulture);
						break;

					case "gear2":
						Gear[1] = float.Parse(Value, CultureInfo.InvariantCulture);
						break;

					case "gear3":
						Gear[2] = float.Parse(Value, CultureInfo.InvariantCulture);
						break;

					case "gear4":
						Gear[3] = float.Parse(Value, CultureInfo.InvariantCulture);
						break;

					case "gear5":
						Gear[4] = float.Parse(Value, CultureInfo.InvariantCulture);
						break;

					case "gear6":
						Gear[5] = float.Parse(Value, CultureInfo.InvariantCulture);
						break;

					case "final_ratio":
						Final = float.Parse(Value, CultureInfo.InvariantCulture);
						break;

					case "drag_coef":
						DragCoef = float.Parse(Value, CultureInfo.InvariantCulture);
						break;

					case "frontal_area":
						FrontalArea = float.Parse(Value, CultureInfo.InvariantCulture);
						break;

					case "tire_diam":
						TireDiam = float.Parse(Value, CultureInfo.InvariantCulture);
						break;
				}
			}

			Weight += Weight2;
			Weight2 = 0;
		}

		public static float Calculate(int RunGear, float CurRPM, float PrevRPM, float CurTime, float PrevTime, out float Spd) {
			float TotalWeight = Weight + Weight2;
			double CurSpeed = CalcSpeed(RunGear, CurRPM);
			double PrevSpeed = CalcSpeed(RunGear, PrevRPM);

			float Hp = Calc(TotalWeight, CurSpeed, PrevSpeed, CurTime, PrevTime);
			float DragHp = CalculateDragHp(CurSpeed);

			Spd = (float)CurSpeed;
			return Hp + DragHp;
		}

		public static float Calculate(float CurSpeed, float PrevSpeed, float CurTime, float PrevTime) {
			float TotalWeight = Weight + Weight2;

			float Hp = Calc(TotalWeight, CurSpeed, PrevSpeed, CurTime, PrevTime);
			float DragHp = CalculateDragHp(CurSpeed);
			return Hp + DragHp;
		}

		static float CalculateDragHp(double CurSpeed) {
			double CurSpeedMPH = CurSpeed * 0.621371;
			return (float)VirtualDyno.DragHorsepower(CurSpeedMPH, DragCoef, FrontalArea);
		}

		public static float Calc(double Mass, double CurSpeed, double PrevSpeed, double CurTime, double PrevTime) {
			//Console.Write("Mass = {0}, CurSpeed = {1}, PrevSpeed = {2}, CurTime = {3}, PrevTime = {4}, HP = ", Mass, (int)CurSpeed, (int)PrevSpeed, CurTime, PrevTime);

			double kmh0 = PrevSpeed;
			double kmh1 = CurSpeed;
			double t = CurTime - PrevTime;
			double m = Mass;

			double a = (kmh1 - kmh0) * (1000.0 / 3600.0) / t;
			double f = m * a;
			double d = (kmh0 * 1000.0 / 3600.0 * t) + 1.0 / 2.0 * a * t * t;
			double w = f * d;
			double p = w / t;

			float HP = (float)((p * 1.34102) / 1000);
			//Console.WriteLine(HP);
			return HP;
		}

		public static double CalcSpeed(int RunGear, float RPM, bool MPH = false) {
			double TransmissionRatio = Gear[RunGear - 1];
			double Speed = RPM * TireDiam / (1.0 * TransmissionRatio * Final * 336.0);

			if (!MPH)
				Speed *= 1.60934;

			return Speed;
		}

		public static float CalcTorque(float PowerHP, float RPM) {
			double PowerKW = PowerHP * 0.7457;
			return (float)(9.5488 * PowerKW / RPM);
		}
	}
}