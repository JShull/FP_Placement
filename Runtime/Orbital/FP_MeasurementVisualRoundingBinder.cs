namespace FuzzPhyte.Placement.OrbitalCamera
{
    using System.Collections.Generic;
    using UnityEngine;
    using FuzzPhyte.Utility;
    using System;
    using TMPro;
    public sealed class FP_MeasurementVisualRoundingBinder : MonoBehaviour
    {
        [Header("Label Source")]
        [SerializeField] private FP_MeasurementLabelUI _labelUI;

        [Header("Inch Measurement Behavior")]
        [Tooltip("Denominator used for inch fractions (16 => nearest 1/16). Common: 8, 16, 32.")]
        [SerializeField] private int _inchDenominator = 16;
        [Tooltip("If true, rounds to nearest fraction step. If false, floors to the step.")]
        [SerializeField] private bool _roundToNearest = true;
        [Tooltip("If true, shows whole number only when fractional part is 0.")]
        [SerializeField] private bool _suppressZeroFraction = true;
        [Tooltip("If true, adds the unit suffix from UnitOfMeasure (ex: in).")]
        [SerializeField] private bool _appendUnitSuffix = true;
        [Header("Optional preferred fraction set")]
        [Tooltip("If non-empty, snap to these preferred fractions instead of strict 1/N stepping. Values should be in [0..1].")]
        [SerializeField] private List<float> _preferredFractions01 = new List<float>(); // ex: 0, 1/16, 1/8, 3/16, ...
        
        [Space]
        [Header("FT Measurement Behavior")]
        [SerializeField] private bool _feetUseInchFractions = true;
        [SerializeField] private bool _suppressZeroInches = true;

        [Space]
        [Header("CM Measurement Behavior")]
        [SerializeField] private int _mmStep = 1;          // 1 = nearest mm, 2 = nearest 2mm, etc.
        [SerializeField] private bool _mmRoundToNearest = true;
        [SerializeField] private bool _suppressZeroMm = true;

        [Space]
        [Header("M Measurement Behavior")]
        [SerializeField] private bool _metersUseCmAndMm = false;
        [SerializeField] private int _cmStep = 1;
        [SerializeField] private bool _cmRoundToNearest = true;
        [SerializeField] private bool _suppressZeroCm = true;

        private void Reset()
        {
            if (_labelUI == null) _labelUI = FindFirstObjectByType<FP_MeasurementLabelUI>();
        }

        private void Awake()
        {
            if (_labelUI == null)
                _labelUI = FindFirstObjectByType<FP_MeasurementLabelUI>();
        }

        private void OnEnable()
        {
            if (_labelUI != null)
                _labelUI.MeasurementChanged += OnMeasurementChanged;
        }

        private void OnDisable()
        {
            if (_labelUI != null)
                _labelUI.MeasurementChanged -= OnMeasurementChanged;
        }

        private void OnMeasurementChanged(TMP_Text label, float valueInUnits, UnitOfMeasure units)
        {
            if (label == null) return;

            switch (units)
            {
                case UnitOfMeasure.Inch:
                    label.text = FormatInchesAsFraction(valueInUnits, units);
                    break;
                case UnitOfMeasure.Feet:
                    label.text = FormatFeetWithInches(valueInUnits, units);
                    break;
                case UnitOfMeasure.Centimeter:
                    label.text = FormatCentimetersAsMmRemainder(valueInUnits, units);
                    break;
                case UnitOfMeasure.Meter:
                    label.text = FormatMetersWithRemainder(valueInUnits, units);
                    break;
            }
           
            // Fallback: do nothing or provide alternate formatting later
        }

        private string FormatInchesAsFraction(float inches, UnitOfMeasure units)
        {
            // Handle negatives just in case
            bool isNeg = inches < 0f;
            float v = Mathf.Abs(inches);

            int whole = Mathf.FloorToInt(v);
            float frac = v - whole;

            // Snap fraction to either preferred fractions or 1/denominator steps
            float snappedFrac01 = (_preferredFractions01 != null && _preferredFractions01.Count > 0)
                ? SnapToPreferred(frac)
                : SnapToStep(frac, _inchDenominator, _roundToNearest);

            // Convert snapped [0..1) to numerator/denominator
            int denom = Mathf.Max(1, _inchDenominator);
            int numer = Mathf.RoundToInt(snappedFrac01 * denom);

            // Carry: 15/16 + rounding might become 16/16 => increment whole
            if (numer >= denom)
            {
                whole += 1;
                numer = 0;
            }

            // Reduce fraction
            if (numer != 0)
            {
                int g = Gcd(numer, denom);
                numer /= g;
                denom /= g;
            }

            string sign = isNeg ? "-" : "";

            string unitSuffix = "";
            if (_appendUnitSuffix)
                unitSuffix = " " + GetSuffix(units);

            if (numer == 0 && _suppressZeroFraction)
            {
                return $"{sign}{whole}{unitSuffix}";
            }

            if (whole == 0)
            {
                // 0 1/16 => "1/16" (unless you want "0 1/16")
                return $"{sign}{numer}/{denom}{unitSuffix}";
            }

            return $"{sign}{whole} {numer}/{denom}{unitSuffix}";
        }
        private string FormatFeetWithInches(float feetValue, UnitOfMeasure units)
        {
            bool isNeg = feetValue < 0f;
            float v = Mathf.Abs(feetValue);

            int wholeFt = Mathf.FloorToInt(v);
            float fracFt = v - wholeFt;

            // remainder feet -> inches (float)
            float inchesFloat = fracFt * 12f;

            string sign = isNeg ? "-" : "";
            string ftSuffix = _appendUnitSuffix ? $" {GetSuffix(UnitOfMeasure.Feet)}" : "";
            string inSuffix = _appendUnitSuffix ? $" {GetSuffix(UnitOfMeasure.Inch)}" : "";

            // If user wants just decimal inches
            if (!_feetUseInchFractions)
            {
                float inchesShown = _roundToNearest ? Mathf.Round(inchesFloat * 100f) / 100f : Mathf.Floor(inchesFloat * 100f) / 100f;

                if (_suppressZeroInches && Mathf.Abs(inchesShown) < 0.0001f)
                    return $"{sign}{wholeFt}{ftSuffix}";

                return $"{sign}{wholeFt}{ftSuffix} {inchesShown:0.##}{inSuffix}";
            }

            // Fraction mode: snap inches remainder to nearest 1/N inch
            int wholeIn = Mathf.FloorToInt(inchesFloat);
            float fracIn = inchesFloat - wholeIn;

            // snap fractional inch to 1/_inchDenominator
            int denom = Mathf.Max(1, _inchDenominator);
            float steps = fracIn * denom;

            float snappedSteps = _roundToNearest ? Mathf.Round(steps) : Mathf.Floor(steps);
            int numer = Mathf.RoundToInt(snappedSteps);

            // Carry within inches: 15/16 rounding => 16/16 => +1 inch
            if (numer >= denom)
            {
                wholeIn += 1;
                numer = 0;
            }

            // Carry to feet: 12 inches => +1 ft
            if (wholeIn >= 12)
            {
                wholeFt += 1;
                wholeIn -= 12;
            }

            // Reduce fraction
            int rDen = denom;
            int rNum = numer;
            if (rNum != 0)
            {
                int g = Gcd(rNum, rDen);
                rNum /= g;
                rDen /= g;
            }

            // Build inch string
            string inchPart;
            if (rNum == 0)
            {
                inchPart = $"{wholeIn}{inSuffix}";
            }
            else if (wholeIn == 0)
            {
                // "1/4 in"
                inchPart = $"{rNum}/{rDen}{inSuffix}";
            }
            else
            {
                // "5 1/4 in"
                inchPart = $"{wholeIn} {rNum}/{rDen}{inSuffix}";
            }

            // Optional suppression: if inches are 0 (and fraction 0), return just feet
            if (_suppressZeroInches && wholeIn == 0 && rNum == 0)
                return $"{sign}{wholeFt}{ftSuffix}";

            return $"{sign}{wholeFt}{ftSuffix} {inchPart}";
        }

        private string FormatCentimetersAsMmRemainder(float cmValue, UnitOfMeasure units)
        {
            bool isNeg = cmValue < 0f;
            float v = Mathf.Abs(cmValue);

            int wholeCm = Mathf.FloorToInt(v);
            float fracCm = v - wholeCm;

            // 1 cm = 10 mm
            float mmRaw = fracCm * 10f;

            int step = Mathf.Max(1, _mmStep);

            float snappedMm = _mmRoundToNearest
                ? Mathf.Round(mmRaw / step) * step
                : Mathf.Floor(mmRaw / step) * step;

            int mm = Mathf.Clamp(Mathf.RoundToInt(snappedMm), 0, 10);

            // Carry: 9.9mm rounding can become 10mm -> +1 cm
            if (mm >= 10)
            {
                wholeCm += 1;
                mm = 0;
            }

            string sign = isNeg ? "-" : "";
            string unitSuffix = _appendUnitSuffix ? $" {GetSuffix(units)}" : ""; // "cm"

            if (mm == 0 && _suppressZeroMm)
                return $"{sign}{wholeCm}{unitSuffix}";

            // show as: "12 cm 3 mm"
            string mmSuffix = _appendUnitSuffix ? $" {GetSuffix(UnitOfMeasure.Millimeter)}" : " mm";
            return $"{sign}{wholeCm}{unitSuffix} {mm}{mmSuffix}";
        }
        private string FormatMetersWithRemainder(float metersValue, UnitOfMeasure units)
        {
            bool isNeg = metersValue < 0f;
            float v = Mathf.Abs(metersValue);

            int wholeM = Mathf.FloorToInt(v);
            float fracM = v - wholeM;

            string sign = isNeg ? "-" : "";
            string mSuffix = _appendUnitSuffix ? $" {GetSuffix(UnitOfMeasure.Meter)}" : "";

            // remainder meters -> centimeters (float)
            float cmFloat = fracM * 100f;

            // Carry handling depends on mode because snapping differs.
            if (_metersUseCmAndMm)
            {
                // Single rounding path:
                // reuse cm->mm formatter for the remainder (ex: "23 cm 4 mm")
                // handle the "100 cm" carry case.
                // format using the same rounding logic by snapping first, then carry.
                // compute snapped remainder in mm units, then split.
                float mmTotal = cmFloat * 10f; // 1 cm = 10 mm => 1 m = 1000 mm

                int mmStep = Mathf.Max(1, _mmStep);
                float snappedMm = _mmRoundToNearest
                    ? Mathf.Round(mmTotal / mmStep) * mmStep
                    : Mathf.Floor(mmTotal / mmStep) * mmStep;

                int mmInt = Mathf.Clamp(Mathf.RoundToInt(snappedMm), 0, 1000);

                if (mmInt >= 1000)
                {
                    wholeM += 1;
                    mmInt = 0;
                }

                int cm = mmInt / 10;
                int mm = mmInt % 10;

                if (cm == 0 && mm == 0 && _suppressZeroCm)
                    return $"{sign}{wholeM}{mSuffix}";

                string cmSuffix = _appendUnitSuffix ? $" {GetSuffix(UnitOfMeasure.Centimeter)}" : " cm";
                string mmSuffix = _appendUnitSuffix ? $" {GetSuffix(UnitOfMeasure.Millimeter)}" : " mm";

                if (mm == 0 && _suppressZeroMm)
                    return $"{sign}{wholeM}{mSuffix} {cm}{cmSuffix}";

                return $"{sign}{wholeM}{mSuffix} {cm}{cmSuffix} {mm}{mmSuffix}";
            }
            else
            {
                // ✅ Snap at centimeters only
                int step = Mathf.Max(1, _cmStep);
                float snappedCm = _cmRoundToNearest
                    ? Mathf.Round(cmFloat / step) * step
                    : Mathf.Floor(cmFloat / step) * step;

                int cm = Mathf.Clamp(Mathf.RoundToInt(snappedCm), 0, 100);

                if (cm >= 100)
                {
                    wholeM += 1;
                    cm = 0;
                }

                if (cm == 0 && _suppressZeroCm)
                    return $"{sign}{wholeM}{mSuffix}";

                string cmSuffix = _appendUnitSuffix ? $" {GetSuffix(UnitOfMeasure.Centimeter)}" : " cm";
                return $"{sign}{wholeM}{mSuffix} {cm}{cmSuffix}";
            }
        }


        private static float SnapToStep(float frac01, int denom, bool roundToNearest)
        {
            denom = Mathf.Max(1, denom);
            float steps = frac01 * denom;

            float snappedSteps = roundToNearest
                ? Mathf.Round(steps)
                : Mathf.Floor(steps);

            return snappedSteps / denom;
        }

        private float SnapToPreferred(float frac01)
        {
            // Find closest preferred fraction in [0..1]
            float best = _preferredFractions01[0];
            float bestDist = Mathf.Abs(frac01 - best);

            for (int i = 1; i < _preferredFractions01.Count; i++)
            {
                float f = Mathf.Clamp01(_preferredFractions01[i]);
                float d = Mathf.Abs(frac01 - f);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = f;
                }
            }

            return Mathf.Clamp01(best);
        }

        private string GetSuffix(FuzzPhyte.Utility.UnitOfMeasure units)
        {
            var s = FP_UtilityData.GetUnitAbbreviation(units);
            if (s!=string.Empty)
                return s;

            // Fallback
            return units.ToString();
        }

        private static int Gcd(int a, int b)
        {
            a = Mathf.Abs(a);
            b = Mathf.Abs(b);
            while (b != 0)
            {
                int t = a % b;
                a = b;
                b = t;
            }
            return Mathf.Max(1, a);
        }
    }
}
