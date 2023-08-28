using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineCalculator {
    static class CylAirmass {
        static float SpecificGasConstant = 287.058f; // J / (Kg * K)
        static float NumOfCylinders = 4;
        static float Displacement = 0.001364f;
        static float PowerStrokesPerRPM = 0.5f;



        /// <param name="RPM">RPM</param>
        /// <param name="ManifoldAbsolutePressure">Pascal</param>
        /// <param name="AmbientAirTemp">Kelvin</param>
        /// <returns></returns>
        public static float CalcAirmass(float RPM, float ManifoldAbsolutePressure, float AmbientAirTemp) {
            float PowerStrokesPerRPM = 0.5f;
            float RoundsPerSecond = RPM / 60.0f;

            float AirDensity = ManifoldAbsolutePressure / (SpecificGasConstant * AmbientAirTemp); // Kg / m3
            float DisplacementPerCyl = (Displacement * PowerStrokesPerRPM) / NumOfCylinders; // m3

            float CalculatedAirMass = (DisplacementPerCyl * RoundsPerSecond) * AirDensity * 1000; // g/s
            float IntakeCyclesPerSecond = RoundsPerSecond * (NumOfCylinders * PowerStrokesPerRPM);
            float CylinderAirmass = (CalculatedAirMass * NumOfCylinders) / IntakeCyclesPerSecond;

            return CylinderAirmass;
        }

        public static float CalcMAF(float RPM, float ManifoldAbsolutePressure, float AmbientAirTemp) {
            float PowerStrokesPerRPM = 0.5f;
            float RoundsPerSecond = RPM / 60.0f;

            float AirDensity = ManifoldAbsolutePressure / (SpecificGasConstant * AmbientAirTemp); // Kg / m3
            float DisplacementPerCyl = (Displacement * PowerStrokesPerRPM) / NumOfCylinders; // m3

            float CalculatedAirMass = (DisplacementPerCyl * RoundsPerSecond) * AirDensity * 1000; // g/s
            //float IntakeCyclesPerSecond = RoundsPerSecond * (NumOfCylinders * PowerStrokesPerRPM);
            //float CylinderAirmass = (CalculatedAirMass * NumOfCylinders) / IntakeCyclesPerSecond;

            return CalculatedAirMass * NumOfCylinders;
        }

        public static float CalcManifoldAbsolutePressure(float RPM, float Airmass, float AmbientAirTemp) {
            float PowerStrokesPerRPM = 0.5f;
            float RoundsPerSecond = RPM / 60.0f;

            float DisplacementPerCyl = (Displacement * PowerStrokesPerRPM) / NumOfCylinders; // m3
            float IntakeCyclesPerSecond = RoundsPerSecond * (NumOfCylinders * PowerStrokesPerRPM);
            float CylinderAirmass = Airmass / NumOfCylinders;

            float CalculatedAirMass = CylinderAirmass * IntakeCyclesPerSecond;
            float AirDensity = CalculatedAirMass / (DisplacementPerCyl * RoundsPerSecond * 1000); // Kg / m3

            float ManifoldAbsolutePressure = AirDensity * SpecificGasConstant * AmbientAirTemp;

            return ManifoldAbsolutePressure;
        }
    }
}
