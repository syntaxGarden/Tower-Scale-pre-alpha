using Godot;
using System;
using System.Collections.Generic;

public partial class Global_variables : Node
{
    public string ScriptDesc { get { return GetScriptDesc(); } }
    internal string GetScriptDesc() { return "This is a global script that will be autoloaded."; }
    private int saved_FOV = 90;
    public int SavedFOV { get { return saved_FOV; } set { saved_FOV = value; } }
    public string GrappleStr { get { return "grapple"; } }
    //private string math_hex = "0x"; //this is for randomising the equations 
    private List<string> math_string_list = new List<string>(14); 
    public List<string> MathProblems { get { return math_string_list; } set { math_string_list = value; } }
    
}
/*

    public void LoadMath()
    {
        //Load the math_hex from the save file
        SolveMathHex();
    }
    private static void GenerateNewMath(int n)
    {
        Random rng = new Random();
        List<int> valid_ints = new List<int>();
        bool unsolved = false;
    }
    private void SolveMathHex() //No parameters, solve the hex directly
    {
        //
    }
    private int SolveMathHex(int i, List<int> x) //Parameters, solves the nth problem using the list of parameters given 
    {
        switch (i) 
        {
            case 1: //Addition and subtraction
                return x[0] + x[1] - x[2];
            case 2: //Multiplcation and division
                return x[0] * x[1] / x[2];
            case 3: //Powers test
                return (int)Math.Pow(x[0],x[2]) + (int)Math.Pow(x[1],x[3]);
            case 4: //Bidmas test
                break;
            case 5: //Basic algebra
                break;
            case 6: //Quadratic equation
                break;
            case 7: //Simultaneous equations
                break;
        }
    }

*/