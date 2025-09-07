namespace IDS.CMF.DataModel
{
    public class ImportCheckboxModel
    {
        private bool _isImportSelected;
        public bool IsImportSelected
        {
            get { return _isImportSelected; }
            set
            {
                _isImportSelected = value;
            }
        }

        private bool _isReferenceObject;
        public bool IsReferenceObject
        {
            get { return _isReferenceObject; }
            set
            {
                _isReferenceObject = value;
            }
        }

        private string _proPlanObjectName;
        public string PlanningObjectName
        {
            get { return _proPlanObjectName; }
            set
            {
                _proPlanObjectName = value;
            }
        }

        private string _idsObjectName;
        public string IdsObjectName
        {
            get { return _idsObjectName; }
            set
            {
                _idsObjectName = value;
            }
        }
    }
}