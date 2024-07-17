using Godot;
using System;
using System.Collections.Generic;

public partial class MathBoard : Node3D
{
    private int HP = 5; 
    private string[] questions; 
    private int[] answers;
    private void ReadyMaths()
    {
        //Global_variables global = (Global_variables)GetNode("/root/Global");
        //global.LoadMath(); //TODO - Need to place LoadMath() call in the ready function for the main game scene so that the complicated maths and string stuff is done alongside any of the other loading.

        //default questions and answers (implement randomisation later)
        questions = new string[4] { 
            "5 ^ 2 + 3 ^ 0 - (2 + 3 x 6 - 5)", //25 + 1 - 15 = 11
            "7α - 15 = 3α + 9", //4α -15 = 9 | 4a = 24 | a = 6
            "(μ - 6) ^ 2 + 3(μ - 5) = μ", //Answer is μ = 5 ± 2 | (x - 6)^2 - x + 3(x - 5) = 0 = x2 - 10x - 21 = (x-7)(x-3) = 0 
            "δ + 2λ = 26\n3δ - λ = 29" //δ = 12, λ = 7
        };
        answers = new int[6] { 11,6,5,2,12,7 };
    }
    public override void _Ready() 
    {
        ReadyMaths();
        GD.PrintRich(questions[0] + " = " + answers[0]);
        GD.PrintRich(questions[1] + " | α = " + answers[1]);
        GD.PrintRich(questions[2] + " | μ = " + answers[2] + " ± " + answers[3]);
        GD.PrintRich(questions[3] + " | δ = " + answers[4] + ", λ = " + answers[5]); 
        //TODO - The super script doesn't work, and will be hard to get working on a RichTextLabel. 
        //Solution - Just make all of the text an image and then place it on the board
    }
}