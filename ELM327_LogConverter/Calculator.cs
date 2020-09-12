using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using VirtualDyno.Core;

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
				string Value = Lines[i].Substring(Line[0].Length).Trim();


				switch (Line[0]) {
					case "car":
						Car = Value;
						break;

					case "weight":
						Weight = float.Parse(Value, CultureInfo.InvariantCulture) * 2.20462f;
						break;

					case "weight2":
						Weight2 = float.Parse(Value, CultureInfo.InvariantCulture) * 2.20462f;
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

		public static float Calculate(int RunGear, float CurRPM, float PrevRPM, float CurTime, float PrevTime) {
			float Hp = (float)Calculations.Horsepower(Weight, CurRPM, PrevRPM, CurTime, PrevTime, TireDiam, Gear[RunGear - 1], Final, false);
			float DragHp = (float)Calculations.DragHorsepower(Calculations.MPH(CurRPM, Gear[RunGear - 1], Final, TireDiam), DragCoef, FrontalArea);

			return Hp + DragHp;
		}
	}
}
