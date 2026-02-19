using System.Windows.Controls;
using Model;

namespace BabySitter
{
    public partial class KidInfoControl : UserControl
    {
        private Parents parent;

        public KidInfoControl(Parents p)
        {
            InitializeComponent();
            parent = p;
        }

        public string KidName => KidNameTextBox.Text;
        public int Age => int.TryParse(KidAgeTextBox.Text, out int a) ? a : 0;
        public string Notes => NotesTextBox.Text;

        public ChildOfParent ToModel()
        {
            return new ChildOfParent
            {
                //IdParent = Parents.Id,
                //FirstName = KidName,
                //DateOfBirth = 
            };
        }
    }
}
