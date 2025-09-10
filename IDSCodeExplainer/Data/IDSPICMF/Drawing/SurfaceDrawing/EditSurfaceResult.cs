using IDS.CMF.DataModel;
using System;
using System.Collections.Generic;

namespace IDS.PICMF.Drawing
{
    public class EditSurfaceResult
    {
        public Dictionary<Guid, PatchData> Surfaces { get; set; } 
            = new Dictionary<Guid, PatchData>();
    }
}
