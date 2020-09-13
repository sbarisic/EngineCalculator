using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ELM327_LogConverter {
	static class Utils {
		static Random Rnd = new Random();

		public static double Lerp(double F1, double F2, double Amt) {
			return F1 * (1.0 - Amt) + F2 * Amt;
		}

		public static double Lerp(double Key1, double Value1, double Key2, double Value2, double KeyAmt) {
			double Range = Key2 - Key1;
			double KeyAmtOffset = KeyAmt - Key1;
			double Amt = KeyAmtOffset / Range;
			return Lerp(Value1, Value2, Amt);
		}

		public static void NoiseReduction(ref float[] Data, int Severity = 1) {
			for (int i = 1; i < Data.Length; i++) {
				int start = (i - Severity > 0 ? i - Severity : 0);
				int end = (i + Severity < Data.Length ? i + Severity : Data.Length);

				float sum = 0;

				for (int j = start; j < end; j++) {
					sum += Data[j];
				}

				float avg = sum / (end - start);
				Data[i] = avg;
			}
		}

		public static int RoundToNearest(int Num, int Nearest, bool RoundDown = true) {
			int Add = 0;

			if (!RoundDown)
				Add += Nearest;

			return ((Num + Add) / Nearest) * Nearest;
		}

		public static Color RandomColor() {
			return Color.FromArgb(Rnd.Next(0, 256), Rnd.Next(0, 256), Rnd.Next(0, 256));
		}

		public static string StripQuotes(string Line) {
			if (Line.StartsWith("\"") && Line.EndsWith("\""))
				return Line.Substring(1, Line.Length - 2);

			return Line;
		}

		public static string ToString(double D) {
			return string.Format("{0:0.000}", D);
		}
	}
}
