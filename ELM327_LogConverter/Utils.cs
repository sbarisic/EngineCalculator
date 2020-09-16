﻿using System;
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

		public static void NoiseReduction(ref double[] Data, int Severity = 1) {
			for (int i = 1; i < Data.Length; i++) {
				int start = (i - Severity > 0 ? i - Severity : 0);
				int end = (i + Severity < Data.Length ? i + Severity : Data.Length);

				double sum = 0;

				for (int j = start; j < end; j++) {
					sum += Data[j];
				}

				double avg = sum / (end - start);
				Data[i] = avg;
			}
		}

		public static double[] ApplyUKF(double[] Data) {
			double[] NewData = new double[Data.Length];

			UKF Filter = new UKF();

			for (int i = 0; i < Data.Length; i++) {
				Filter.Update(new[] { Data[i] });
				NewData[i] = Filter.getState()[0];
			}

			return NewData;
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

		public static double Distance(double A, double B) {
			return Math.Abs(A - B);
		}

		public static IEnumerable<Color> GetColors() {
			string[] ColorStrings = new[] {
				"F2F3F4", "222222", "F3C300",
				"875692", "F38400", "A1CAF1",
				"BE0032", "C2B280", "848482",
				"008856", "E68FAC", "0067A5",
				"F99379", "604E97", "F6A600",
				"B3446C", "DCD300", "882D17",
				"8DB600", "654522", "E25822",
				"2B3D26"
			};

			for (int i = 0; i < ColorStrings.Length; i++) {
				byte[] ColorBytes = ToBytes(ColorStrings[i]);
				yield return Color.FromArgb(ColorBytes[2], ColorBytes[1], ColorBytes[0]);
			}
		}

		public static byte[] ToBytes(string HexString) {
			return Enumerable.Range(0, HexString.Length).Where(x => x % 2 == 0).Select(x => Convert.ToByte(HexString.Substring(x, 2), 16)).ToArray();
		}
	}
}