using IDS.Core.DataTypes;
using IDS.Core.Utilities;
using Rhino.Geometry;
using System.Collections.Generic;

namespace IDS.CMF.Operations
{
    public class RemoveNoiseDisjointedShellEditor : DisjointedShellEditor
    {
        protected override void PreprocessDataModel(Mesh disjointableMesh, PreprocessMode preprocessMode, out IEnumerable<Mesh> meshesKeep,
            out IEnumerable<Mesh> meshesRemove)
        {
            if (preprocessMode != PreprocessMode.Preprocess)
            {
                base.PreprocessDataModel(disjointableMesh, preprocessMode, out meshesKeep, out meshesRemove);
                return;
            }

            MeshUtilities.RemoveNoiseShellsUsingStatistics(disjointableMesh, out meshesKeep, out meshesRemove, true, DisjointedShellEditorConstants.AcceptanceNumSigma,
                DisjointedShellEditorConstants.AcceptanceThicknessRatio, DisjointedShellEditorConstants.AcceptanceVolumeHardLimit, DisjointedShellEditorConstants.AcceptanceAreaHardLimit);
        }
    }
}
