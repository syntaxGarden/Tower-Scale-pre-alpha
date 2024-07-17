using Godot;
using System;

public partial class pronouns_test : Node2D
{
    public override void _Ready()
    {
        //Need to set the values of the initial pronouns and also set the values of any saved pronouns
    }
    private void OnPronounListItemSelected(int n) {
		Label speech_node = (Label)GetNode("speech");
		speech_node.Text = "Boss dialogue examples:\n\n";
		
		Pronouns P = (Pronouns)GetNode("/root/Pronouns");
		P.ChosenPronoun = n;
		//[They,have] escaped containment. Somebody go after [them], [their] equipment is still secure but [they,are] on [their] way.\n\n[Have,they] awakened yet? Those powers aren't even [theirs], [they,have] no right to use them.\n\n[They,have] got [themself] in a whole heap of trouble now. How stupid can [they] possibly be?
		speech_node.Text += P.X(0,true) + P.PS("have",true) + " escaped containment. Somebody go after " + P.X(1) + ", " + P.X(2) + " equipment is still secure but " + P.X(0) + P.PS("are",true) + " on " + P.X(2) + " way.\n\n" + P.PS("have",false,true) + " " + P.X(0) + " awakened yet? Those powers aren't even " + P.X(3) + ", " + P.X(0) + " " + P.PS("have") + " no right to use them.\n\n" + P.X(0,true) + P.PS("have",true) + " gotten " + P.X(4) + " into a heap of trouble now. How stupid can " + P.X(0) + " possibly be?";
	}

	private void _on_button_pressed() {
		GetTree().ChangeSceneToFile("res://scenes/debug_menu.tscn");
	}
}
