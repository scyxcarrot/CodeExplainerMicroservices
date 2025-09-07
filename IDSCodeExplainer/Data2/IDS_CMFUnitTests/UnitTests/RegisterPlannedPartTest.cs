using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)

    [TestClass]
    public class RegisterPlannedPartTests : RegisterPartTests
    {
        //Unit tests for Register parts from Planned to Original position
        //Source = Planned Part; Target = Original Part

        [TestMethod]       
        public void Original_Part_Is_Registered_To_Correct_Position_And_With_Correct_Geometry()
        {
            Target_Part_Is_Registered_To_Correct_Position_And_With_Correct_Geometry(_plannedInputInformation, _originalInputInformation);
        }

        [TestMethod]
        public void Original_Part_Is_Not_Registered_If_Original_Part_Does_Not_Exist_In_Case()
        {
            //arrange
            Prepare_Default_Case(false, true);

            Target_Part_Is_Not_Registered_If_Target_Part_Does_Not_Exist_In_Case(_plannedInputInformation, _originalInputInformation);
        }

        [TestMethod]
        public void No_Registeration_Take_Place_If_Input_Contains_Corresponding_Original_Part()
        {
            No_Registeration_Take_Place_If_Input_Contains_Corresponding_Target_Part(_plannedInputInformation, _originalInputInformation);
        }

        [TestMethod]
        public void Registered_Original_Part_Maintains_Its_Transformation_Matrix()
        {
            Registered_Target_Part_Maintains_Its_Transformation_Matrix(_plannedInputInformation, _originalInputInformation);
        }

        [TestMethod]
        public void Original_Part_Is_Not_Registered_If_Planned_Part_Does_Not_Exist_In_Case()
        {
            //arrange
            Prepare_Default_Case(true, false);

            Target_Part_Is_Not_Registered_If_Source_Part_Does_Not_Exist_In_Case(_plannedInputInformation, _originalInputInformation);
        }
    }

#endif
}