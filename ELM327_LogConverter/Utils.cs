using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ELM327_LogConverter {
	static class Utils {

		public static float Lerp(float F1, float F2, float Amt) {
			return F1 * (1.0f - Amt) + F2 * Amt;
		}

		public static float Lerp(float Key1, float Value1, float Key2, float Value2, float KeyAmt) {
			float Range = Key2 - Key1;
			float KeyAmtOffset = KeyAmt - Key1;
			float Amt = KeyAmtOffset / Range;
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
	}
}
