using System.Drawing;

namespace IDS.CMF.DataModel
{
    public class BoneThicknessAnalysisARTComponentScreenshots
    {
        public Image LeftView { get; }
        public Image RightView { get; }

        public BoneThicknessAnalysisARTComponentScreenshots(Image leftView, Image rightView)
        {
            LeftView = leftView;
            RightView = rightView;
        }
    }
}
