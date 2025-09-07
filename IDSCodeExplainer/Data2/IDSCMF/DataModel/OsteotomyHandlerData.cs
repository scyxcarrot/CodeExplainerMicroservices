using IDS.CMF.Common;
using IDS.CMF.Constants;
using Rhino.Collections;

namespace IDS.CMF.DataModel
{
    public class OsteotomyHandlerData : ISerializable<ArchivableDictionary>
    {
        public string OsteotomyType { get; private set; }
        public double OsteotomyThickness { get; private set; }
        public string[] HandlerIdentifier { get; private set; }
        public double[,] HandlerCoordinates { get; private set; }

        public string SerializationLabel => "OsteotomyHandlerData";

        private readonly string KeyOsteotomyType = AttributeKeys.KeyOsteotomyType;
        private readonly string KeyOsteotomyThickness = AttributeKeys.KeyOsteotomyThickness;
        private readonly string KeyOsteotomyHandlerIdentifier = AttributeKeys.KeyOsteotomyHandlerIdentifier;
        private readonly string KeyOsteotomyHandlerCoordinate = AttributeKeys.KeyOsteotomyHandlerCoordinate;

        public OsteotomyHandlerData()
        {
            OsteotomyType = null;
            OsteotomyThickness = 0.0;
            HandlerIdentifier = null;
            HandlerCoordinates = null;
        }

        public OsteotomyHandlerData(string osteotomyType, double osteotomyThickness, string[] handlerIdentifier, double[,] handlerCoordinates)
        {
            OsteotomyType = osteotomyType;
            OsteotomyThickness = osteotomyThickness;
            HandlerIdentifier = handlerIdentifier;
            HandlerCoordinates = handlerCoordinates;
        }

        public bool DeSerialize(ArchivableDictionary serializer)
        {
            if (serializer.TryGetString(KeyOsteotomyType, out var osteotomyType))
            {
                OsteotomyType = osteotomyType;
            }

            if (serializer.TryGetDouble(KeyOsteotomyThickness, out var osteotomyThickness))
            {
                OsteotomyThickness = osteotomyThickness;
            }

            if (serializer.TryGetValue(KeyOsteotomyHandlerIdentifier, out var handlerIdentifier))
            {
                HandlerIdentifier = (string[])handlerIdentifier;
            }

            if (serializer.TryGetValue(KeyOsteotomyHandlerCoordinate, out double[,] handlerCoordinate))
            {
                HandlerCoordinates = handlerCoordinate;
            }

            return true;
        }

        public bool Serialize(ArchivableDictionary serializer)
        {
            serializer.Set(KeyOsteotomyType, OsteotomyType);
            serializer.Set(KeyOsteotomyThickness, OsteotomyThickness);
            serializer.Set(KeyOsteotomyHandlerIdentifier, HandlerIdentifier);
            serializer.Set(KeyOsteotomyHandlerCoordinate, HandlerCoordinates);

            return true;
        }

        public bool ClearSerialized(ArchivableDictionary serializer)
        {
            if (serializer.ContainsKey(KeyOsteotomyType))
            {
                serializer.Remove(KeyOsteotomyType);
            }

            if (serializer.ContainsKey(KeyOsteotomyThickness))
            {
                serializer.Remove(KeyOsteotomyThickness);
            }

            if (serializer.ContainsKey(KeyOsteotomyHandlerIdentifier))
            {
                serializer.Remove(KeyOsteotomyHandlerIdentifier);
            }

            if (serializer.ContainsKey(KeyOsteotomyHandlerCoordinate))
            {
                serializer.Remove(KeyOsteotomyHandlerCoordinate);
            }

            return true;
        }
    }
}
