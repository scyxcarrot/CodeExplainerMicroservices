using IDS.CMF.V2.Constants;
using IDS.CMF.V2.DataModel;
using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using System;

namespace IDS.CMF.V2.ScrewQc
{
    public abstract class ScrewInfoRecord : ICloneable
    {
        protected ScrewInfoRecord(IScrewQcData screw)
        {
            Id = screw.Id;
            Index = screw.Index;
            ScrewType = screw.ScrewType;
            HeadPoint = new IDSPoint3D(screw.HeadPoint);
            TipPoint = new IDSPoint3D(screw.TipPoint);
        }

        protected ScrewInfoRecord(ScrewInfoRecord record)
        {
            Id = record.Id;
            Index = record.Index;
            ScrewType = record.ScrewType;
            HeadPoint = record.HeadPoint;
            TipPoint = record.TipPoint;
        }

        protected ScrewInfoRecord(CommonScrewSerializableDataModel data)
        {
            Id = data.Id;
            Index = data.Index;
            ScrewType = data.ScrewType;
            HeadPoint = data.HeadPoint;
            TipPoint = data.TipPoint;
        }

        public Guid Id { get; }
        public int Index { get; }
        public string ScrewType { get;}
        public IDSPoint3D HeadPoint { get;}
        public IDSPoint3D TipPoint { get;}
        public abstract Guid CaseGuid { get; }
        public abstract string CaseName { get; }
        public abstract int NCase { get; }
        public abstract bool IsGuideFixationScrew { get;}

        public virtual bool IsSameScrewProperties(ScrewInfoRecord otherRecord)
        {
            if (Id != otherRecord.Id)
            {
                return false;
            }
            
            if (HeadPoint.DistanceTo(otherRecord.HeadPoint) > ScrewQcConstants.Epsilon3Decimal)
            {
                return false;
            }

            if (TipPoint.DistanceTo(otherRecord.TipPoint) > ScrewQcConstants.Epsilon3Decimal)
            {
                return false;
            }

            if (ScrewType != otherRecord.ScrewType)
            {
                return false;
            }

            if (IsGuideFixationScrew != otherRecord.IsGuideFixationScrew)
            {
                return false;
            }

            return true;
        }

        public ScrewInfoRecord CastedClone()
        {
            return (ScrewInfoRecord) Clone();
        }

        public abstract string GetScrewNumber();

        public abstract string GetScrewNumberForScrewQcBubble();

        public abstract object Clone();

        protected void FillCommonScrewSerializableDataModel(CommonScrewSerializableDataModel data)
        {
            data.Id = Id;
            data.Index = Index;
            data.ScrewType = ScrewType;
            data.HeadPoint = HeadPoint;
            data.TipPoint = TipPoint;
        }
    }
}
