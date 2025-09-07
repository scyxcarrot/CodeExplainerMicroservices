using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;

namespace IDS.Glenius.Query
{
    public class ScrewInfoProvider
    {
        public string GetScrewBrand(Screw screw)
        {
            switch (screw.ScrewType)
            {
                case ScrewType.TYPE_3Dot5_LOCKING:
                    return "Synthes";
                case ScrewType.TYPE_4Dot0_LOCKING:
                case ScrewType.TYPE_4Dot0_NONLOCKING:
                    return "OOOS";
                default:
                    return "ERROR, SCREW TYPE NOT FOUND";
            }
        }

        public string GetScrewReferenceNumber(Screw screw)
        {
            switch (screw.ScrewType)
            {
                case ScrewType.TYPE_3Dot5_LOCKING:
                        return Get3Dot5LockingScrewReferenceNumber(screw);
                case ScrewType.TYPE_4Dot0_LOCKING:
                    return Get4dot0LockingScrewReferenceNumber(screw);
                case ScrewType.TYPE_4Dot0_NONLOCKING:
                    return Get4dot0NonLockingScrewReferenceNumber(screw);
                default:
                    return "INVALID SCREW!";
            }
        }

        public string Get3Dot5LockingScrewReferenceNumber(Screw screw)
        {
            if (screw.ScrewType == ScrewType.TYPE_3Dot5_LOCKING)
            {
                return $"4130{screw.TotalLength:F0}";
            }

            return "INVALID SCREW!";
        }

        private string Get4dot0LockingScrewReferenceNumber(Screw screw)
        {
            if (screw.ScrewType == ScrewType.TYPE_4Dot0_LOCKING)
            {
                return $"PC40TL0{screw.TotalLength:F0}";
            }

            return "INVALID SCREW!";
        }

        private string Get4dot0NonLockingScrewReferenceNumber(Screw screw)
        {
            if (screw.ScrewType == ScrewType.TYPE_4Dot0_NONLOCKING)
            {
                return $"PS40TN0{screw.TotalLength:F0}";
            }

            return "INVALID SCREW!";
        }

        public string GetScrewLockingType(Screw screw)
        {
            switch (screw.ScrewType)
            {
                case ScrewType.TYPE_3Dot5_LOCKING:
                case ScrewType.TYPE_4Dot0_LOCKING:
                    return "Locking";
                case ScrewType.TYPE_4Dot0_NONLOCKING:
                    return "Non-Locking";
                default:
                    return "INVALID SCREW!";
            }
        }
    }
}
