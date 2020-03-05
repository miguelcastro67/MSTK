using System;

namespace MSTK.Core.UI
{
    public class MenuItem
    {
        public MenuItem()
        {
        }

        public MenuItem(string menuChoice, Action choiceAction)
        {
            MenuChoice = menuChoice;
            ChoiceAction = choiceAction;
        }

        public string MenuChoice { get; set; }
        public Action ChoiceAction { get; set; }
    }
}
