using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace EngineCalculator {
    class Dat {
        public float RPM;
        public float MAP;
        public float MAT;

        public Dat(float RPM, float MAP, float MAT) {
            this.RPM = RPM;
            this.MAP = MAP;
            this.MAT = MAT;
        }
    }

    class Program {
        static float SpecificGasConstant = 287.058f; // J / (Kg * K)

        static void RunForms() {
            Thread FormThread = new Thread(() => {
                Application.EnableVisualStyles();
                TableConvert TC = new TableConvert();

                Application.Run(TC);
                Environment.Exit(0);
            });

            FormThread.SetApartmentState(ApartmentState.STA);
            FormThread.IsBackground = false;
            FormThread.Start();
        }

        static void Main(string[] args) {
            RunForms();
            //Main2(args);

            // 0	0.08	0.24	0.32	0.38	0.39	0.48	0.56	0.64

            float[] Airmasses = new float[] {
                0.1f, 0.14f, 0.18f, 0.2f, 0.22f, 0.24f, 0.26f, 0.28f, 0.3f, 0.32f, 0.34f, 0.36f, 0.38f, 0.4f, 0.42f, 0.44f, 0.46f, 0.48f, 0.5f, 0.52f, 0.54f, 0.56f, 0.58f, 0.6f, 0.62f, 0.64f, 0.7f, 0.75f, 0.8f, 0.85f, 0.9f, 0.95f, 1f };

            foreach (var AM in Airmasses) {
                float RPMM = 3000;
                float TMPP = ToKelvin(30);

                float CMAP = CylAirmass.CalcManifoldAbsolutePressure(RPMM, AM, TMPP);
                float CAM = CylAirmass.CalcAirmass(RPMM, CMAP, TMPP);
                float CMAF = CylAirmass.CalcMAF(RPMM, CMAP, TMPP);

                Console.WriteLine("{0:0.00} [g/cyl] = {1:0.00} [kPa] = {2:0.00} [g/cyl] = {3:0.00} [g/s]", AM, CMAP / 1000, CAM, CMAF);
            }

            /*Dat[] Data = new Dat[] {
				new Dat(2500, 83, 22), // 0.201, 16
				new Dat(3000, 103, 20), // 0.308, 32
				new Dat(3500, 113, 19), // 0.355, 44
				new Dat(4000, 113, 18), // 0.350, 47
				new Dat(5000, 142, 17), // 0.497, 90
				new Dat(6000, 218, 16), // 0.777, 163
				new Dat(6900, 231, 16) ,// 0.864, 200

				new Dat(5300, 238, 31) // 0.844, 148
			};

			foreach (var D in Data) {
				float MAF = CylAirmass.CalcMAF(D.RPM, D.MAP * 1000, ToKelvin(D.MAT));
				Console.WriteLine("{0} RPM = {1} g/s", D.RPM, MAF);

				float CylAir = CylAirmass.CalcAirmass(D.RPM, D.MAP * 1000, ToKelvin(D.MAT));
				Console.WriteLine("{0} RPM = {1} g/cyl", D.RPM, CylAir);

				Console.WriteLine();
			}*/

            Console.ReadLine();
        }

        static void Main2(string[] args) {
            float NumOfCylinders = 4;
            float RPM = 6000; // RPM
            float Bore = 72.5f * 0.001f; // mm
            float Stroke = 82.6f * 0.001f; // mm
            float CompressionRatio = 9.5f;

            float AFR = 14.7f; // Ideal 14.7 / 1
            float Boost = 0.0f; // bar

            float PowerStrokesPerRPM = 0.5f;
            float Efficiency = 30 / 100.0f;

            float RoundsPerSecond = RPM / 60.0f;
            float IntakeCyclesPerSecond = RoundsPerSecond * (NumOfCylinders * PowerStrokesPerRPM);

            float GasolineEnergyDensity = 46400; // J/g

            float PistonRadius = Bore / 2;
            float Displacement = (float)(Math.PI * (PistonRadius * PistonRadius) * Stroke * NumOfCylinders);

            float Altitude = 200; // m above sea level
            float AmbientAirTemp = ToKelvin(21); // Kelvin
            float AmbientAirPressure = PressureAtAlt(AmbientAirTemp, Altitude); // 1.0f * 100000; // Pascals

            float MAP = AmbientAirPressure + ToPascal(Boost);

            // https://www.omnicalculator.com/physics/air-density
            float AirDensity = MAP / (SpecificGasConstant * AmbientAirTemp); // Kg / m3
            float DisplacementPerCyl = (Displacement * PowerStrokesPerRPM) / NumOfCylinders; // m3

            float CalculatedAirMass = (DisplacementPerCyl * RoundsPerSecond) * AirDensity * 1000; // g/s
            float CalculatedFuel = CalculatedAirMass / AFR;

            float CylinderAirmass = (CalculatedAirMass * NumOfCylinders) / IntakeCyclesPerSecond;

            // TODO: Fix, not accounting for air
            float CalculatedPower = (CalculatedFuel * GasolineEnergyDensity) / 1000.0f; // kW

            Console.WriteLine("{0} RPM @ {1} m above sea level, ambient air pressure {2} Pa ({3} bar) @ {4} °C", RPM, Altitude, AmbientAirPressure, ToBar(AmbientAirPressure), ToCelsius(AmbientAirTemp));
            Console.WriteLine("Manifold Abs Pressure     [bar]: {0}", ToBar(MAP));
            Console.WriteLine("Boost                     [bar]: {0}", Boost);
            Console.WriteLine("Air mass per cylinder       [g]: {0}", CylinderAirmass);
            Console.WriteLine("Mass air flow per cyl     [g/s]: {0}", CalculatedAirMass);
            Console.WriteLine("Mass air flow total       [g/s]: {0}", CalculatedAirMass * NumOfCylinders);
            Console.WriteLine("Fuel mass per cyl         [g/s]: {0}", CalculatedFuel);
            Console.WriteLine("Fuel mass                 [g/s]: {0}", CalculatedFuel * NumOfCylinders);
            Console.WriteLine("Power per cylinder         [kW]: {0}", CalculatedPower);
            Console.WriteLine("Power                      [kW]: {0}", CalculatedPower * NumOfCylinders);
            Console.WriteLine("Power                      [hp]: {0}", ToHP(CalculatedPower * NumOfCylinders));

            Console.WriteLine();
            Console.WriteLine("Engine efficiency           [%]: {0}", Efficiency * 100);
            Console.WriteLine("Power corrected            [kW]: {0}", (CalculatedPower * NumOfCylinders) * Efficiency);
            Console.WriteLine("Power corrected            [hp]: {0}", ToHP((CalculatedPower * NumOfCylinders) * Efficiency));

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

        static float ToPascal(float Bar) {
            return Bar * 100000;
        }

        static float ToHP(float kW) {
            return kW * 1.34102f;
        }
    }
}
