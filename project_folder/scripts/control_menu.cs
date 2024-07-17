using Godot;
using System;
using System.Globalization;

//FIXME - Come back here when I have a better idea of what the menu should look like 

public partial class control_menu : Node2D
{
    /*
    Will need a list of the actions
    ~forward, backward, left, right
    ~jump, arm, dodge, slow, reload, change_arm
    ~interact, pause
    Any other action on the input map are to be undisturbed
    */
    private bool choosing = false;
    private bool changed = false;
    private static readonly string[] action_names =  new string[11]{
        "forward","backward","left","right",
        "jump","arm","dodge","slow","reload","interact",
        "change_arm",
    };
    private static readonly string[] display_names = new string[11]{
        "Forward","Backward","Left","Right",
        "Jump","Grapple/Shoot","Dodge","Slowdown","Reload","Interact",
        "Next Weapon"
    };
    private int num_of_actions = action_names.Length; 
    private InputEvent[] events = new InputEvent[11];
    public override void _Ready()
    {
        //First step, grab the different controls arrays, and place them in the right places.   
        
        if (action_names.Length != display_names.Length) { 
            //If the lists for the controls are wrong, this will print that and close the game
            GD.Print("[color=RED]ERROR[color=WHITE]: control_menu.cs unequal name array lengths");
            GetTree().Quit(); return; 
        }
        
        for (int i = 0; i < num_of_actions; i++) {
            events[i] = InputMap.ActionGetEvents(action_names[i])[0];
            GD.Print(display_names[i] + " - " + events[i]);
        }
    }
    public override void _Input(InputEvent @event)
    {
        if (choosing) {
            //Process the input event and use the ones that actually work
            //For the moment, there is only mouse and keyboard support, but I will think about controller after the game is properly finished
            //However, if it's easier to add controls without having to separate 
            
            //Acceptable keyboard and mouse inputevents are InputEventKey and InputEventMouseButton
            InputEventKey key_event = @event as InputEventKey;
            InputEventMouseButton mouse_event = @event as InputEventMouseButton;
            if (key_event != null) {
                //Register key event
            } else if (mouse_event != null) {
                //Register mouse event
            } //Else, ignore and move on 
        }
    }
    private void OnExitPressed() {
        GetTree().ChangeSceneToFile("res://scenes/main_menu.tscn");
    }
}
