using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineCalculator {
	class Program {
		static void Main(string[] args) {
			float NumOfCylinders = 4;
			float RPM = 3000; // RPM
			float Bore = 72.5f * 0.001f; // mm
			float Stroke = 82.6f * 0.001f; // mm
			float CompressionRatio = 9.5f;
			float AFR = 12.0f; // Ideal 14.7 / 1
			float PowerStrokesPerRPM = 0.5f;
			float Efficiency = 30 / 100.0f;

			float GasolineEnergyDensity = 46400; // J/g

			float PistonRadius = Bore / 2;
			float Displacement = (float)(Math.PI * (PistonRadius * PistonRadius) * Stroke * NumOfCylinders);

			float Altitude = 200; // m above sea level
			float AmbientAirTemp = ToKelvin(21); // Kelvin
			float AmbientAirPressure = PressureAtAlt(AmbientAirTemp, Altitude); // 1.0f * 100000; // Pascals
			float SpecificGasConstant = 287.058f; // J / (Kg * K)

			float RoundsPerSecond = RPM / 60.0f;
			float IntakePressure = AmbientAirPressure;

			// https://www.omnicalculator.com/physics/air-density
			float AirDensity = IntakePressure / (SpecificGasConstant * AmbientAirTemp); // Kg / m3
			float DisplacementPerCyl = (Displacement * PowerStrokesPerRPM) / NumOfCylinders; // m3

			float CalculatedAirMass = (DisplacementPerCyl * RoundsPerSecond) * AirDensity * 1000; // g/s
			float CalculatedFuel = CalculatedAirMass / AFR;

			float CalculatedPower = (CalculatedFuel * GasolineEnergyDensity) / 1000.0f; // kW

			Console.WriteLine("{0} RPM @ {1} m above sea level, ambient air pressure {2} Pa ({3} bar) @ {4} °C", RPM, Altitude, AmbientAirPressure, ToBar(AmbientAirPressure), ToCelsius(AmbientAirTemp));
			Console.WriteLine("Air mass per cylinder [g/s]: {0}", CalculatedAirMass);
			Console.WriteLine("Air mass total        [g/s]: {0}", CalculatedAirMass * NumOfCylinders);
			Console.WriteLine("Fuel mass per cyl     [g/s]: {0}", CalculatedFuel);
			Console.WriteLine("Fuel mass             [g/s]: {0}", CalculatedFuel * NumOfCylinders);
			Console.WriteLine("Power per cylinder     [kW]: {0}", CalculatedPower);
			Console.WriteLine("Power                  [kW]: {0}", CalculatedPower * NumOfCylinders);
			Console.WriteLine("Power                  [hp]: {0}", ToHP(CalculatedPower * NumOfCylinders));

			Console.WriteLine();
			Console.WriteLine("Engine efficiency       [%]: {0}", Efficiency * 100);
			Console.WriteLine("Power corrected        [kW]: {0}", (CalculatedPower * NumOfCylinders) * Efficiency);
			Console.WriteLine("Power corrected        [hp]: {0}", ToHP((CalculatedPower * NumOfCylinders) * Efficiency));

			Console.ReadLine();
		}

		static float LitersToMeters3(float L) {
			return L / 1000;
		}

		static float Meters3ToLiters(float m3) {
			return m3 * 1000;
		}

		static float CubicFeetPerMinuteToGramsPerSecondAir(float CFM) {
			return CFM * 0.5549f;
		}

		static float GramsPerSecondAirToCubicFeetPerMinute(float CFM) {
			return CFM * 1.80212650928f;
		}

		static float PressureAtAlt(float Temperature, float Altitude) {
			double PressureAtSea = 101325.0f;
			double R = 8.31432;
			double M = 0.0289644;
			double g = 9.80665;

			if (Altitude <= 0)
				return (float)PressureAtSea;
			else if (Altitude < 11000) {
				var e = -0.0065;
				var i = 0;
				return (float)(PressureAtSea * Math.Pow(Temperature / (Temperature + (e * (Altitude - i))), (g * M) / (R * e)));
			} else {
				if (Altitude <= 20000) {
					var e = -0.0065;
					var i = 0;
					var f = 11000;
					var a = PressureAtSea * Math.Pow(Temperature / (Temperature + (e * (f - i))), (g * M) / (R * e));
					var c = Temperature + (11000 * (-0.0065));

					return (float)(a * Math.Exp(((-g) * M * (Altitude - f)) / (R * c)));
				}
			}

			throw new Exception("Wat");
		}

		static float ToCelsius(float Kelvin) {
			return Kelvin - 273.15f;
		}

		static float ToKelvin(float Celsius) {
			return Celsius + 273.15f;
		}

		static float ToBar(float Pascal) {
			return Pascal / 100000;
		}

		static float ToHP(float kW) {
			return kW * 1.34102f;
		}
	}
}
