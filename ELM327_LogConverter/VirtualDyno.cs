using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ELM327_LogConverter {
	static class VirtualDyno {
		public static double MPH(double EngineRPM, double TransmissionRatio, double FinalDriveRatio, double TireDiameter) {
			return EngineRPM * TireDiameter / (1.0 * TransmissionRatio * FinalDriveRatio * 336.0);
		}

		public static double MPH(double Kmh) {
			return Kmh * 0.621371;
		}

		public static double Horsepower(double TotalWeight, double CurrentEngineRPM, double PrevEngineRPM, double CurrentTime, double PrevTime, double TireDiameter, double TransmissionRatio, double FinalGearRatio, bool IsMetric) {
			double TWeight = IsMetric ? Weight_KilogramsToPounds(TotalWeight) : TotalWeight;

			return Horsepower(TWeight, CurrentEngineRPM, PrevEngineRPM, CurrentTime, PrevTime, TireDiameter, TransmissionRatio, FinalGearRatio);
		}

		public static double DragHorsepower(double MPH, double DragCoefficient, double FrontalAreaFTSQ) {
			return DragCoefficient * FrontalAreaFTSQ * (Math.Pow(MPH, 3.0) / 150000.0);
		}

		public static double SAECorrectionFactor(double Barometer, double AtmosphereTemp, bool Metric) {
			double num = AtmosphereTemp;
			double num2 = Barometer;
			if (Metric) {
				num = Temperature_CelciusToFarenheit(num);
				num2 = Barometer_BarToInHg(num2);
			}
			return 1.18 * (990.0 / (num2 * 33.86) * Math.Sqrt((0.55555555555555558 * (num - 32.0) + 273.0) / 298.0)) - 0.18;
		}

		public static int Weight_PoundsToKilograms(int Pounds) {
			return Convert.ToInt32(Math.Round((double)Pounds / 2.2, 0));
		}

		public static double Weight_KilogramsToPounds(double Kilograms) {
			return Kilograms * 2.20462262185;
		}

		public static double Temperature_CelciusToFarenheit(double Temperture) {
			return Math.Round(Temperture * 1.8 + 32.0, 0);
		}

		public static double Temperature_FarenheitToCelcius(double Temperture) {
			return Math.Round((Temperture - 32.0) * 0.55555555555555558, 0);
		}

		public static double Barometer_InHgToBar(double InHg) {
			return Math.Round(InHg / PRESSURE_BAR_INHG_CONVERSIONFACTOR, 3);
		}

		public static double Barometer_BarToInHg(double Bar) {
			return Math.Round(Bar * PRESSURE_BAR_INHG_CONVERSIONFACTOR, 3);
		}

		private static double Horsepower(double TotalWeight, double CurrentEngineRPM, double PrevEngineRPM, double CurrentTime, double PrevTime, double TireDiameter, double TransmissionRatio, double FinalGearRatio) {
			double dt = CurrentTime - PrevTime;
			
			double x = 3000.0 / (3000.0 * (TireDiameter / 2.0) / (TransmissionRatio * FinalGearRatio * 168.0));
			double num = Math.Pow(1.4666666666666666, 2.0);
			double num2 = TotalWeight / 32.17;

			double num3 = Math.Pow(x, 2.0);
			double RPMPerSec = (CurrentEngineRPM - PrevEngineRPM) / dt;

			double num5 = (CurrentEngineRPM + PrevEngineRPM) / 2.0 / 550.0;
			return num * (num2 / num3) * RPMPerSec * num5;
		}

		private static double PRESSURE_BAR_INHG_CONVERSIONFACTOR = 29.5299830714;
	}
}
