using System;
using System.Collections.Generic;
using System.Text;

namespace MSTK.Core.UI
{
    public class ConsoleHelper
    {
        public void ShowMenu(MenuItem[] menuItems)
        {
            bool exit = false;
            while (!exit)
            {
                Console.WriteLine();
                Console.WriteLine("Make a selection:");
                for (int i = 0; i < menuItems.Length; i++)
                    Console.WriteLine("{0} - {1}", i + 1, menuItems[i].MenuChoice);
                Console.WriteLine("0 - Exit");

                string choice = Console.ReadLine();
                bool tryChoice = int.TryParse(choice, out int choiceValue);
                if (tryChoice && choiceValue <= menuItems.Length)
                {
                    if (choiceValue > 0)
                        menuItems[choiceValue - 1].ChoiceAction.Invoke();
                    else
                        exit = true;
                }
            }
        }
    }
}
