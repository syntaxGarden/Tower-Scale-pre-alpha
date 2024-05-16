using Godot;
using System;

public partial class Pronouns : Node 
{
    private static readonly string[,] variants = new string[9,5] {
		{"he",   "him",   "his",   "his",   "himself"},
		{"she",  "her",   "her",   "hers",  "herself"},
		{"they", "them",  "their", "theirs","themself"}, 
		{"it",   "it",    "its",   "its",   "itself"},
		{"ve",   "ver",   "vis",   "vis",   "verself"},
		{"xe",   "xem",   "xyr",   "xyrs",  "xemself"},
		{"ey",   "em",    "eir",   "eirs",  "eirself"},
		{"He",   "Him",   "His",   "His",   "Himself"},
		{"shkle","shkler","shkler","shklis","shklimself"}
	};
    enum Ha {s,ve}; //Does the pronoun use has or have (I'm very clever)
	private static readonly Ha[] hashave_array = new Ha[9] { Ha.s, Ha.s, Ha.ve, Ha.s, Ha.s, Ha.s, Ha.ve, Ha.s, Ha.s };
	//TODO - When (maybe) implementing custom pronouns, will need to have a place to store the customs. 3 should do it 
	private int chosen = 0;
	public int ChosenPronoun { get { return chosen; } set { chosen = value; } }
	public string X(int form, bool capitalise = false) {
		string ret = variants[chosen, form];
		return capitalise ? ret.Capitalize() : ret;
	}
	public string PS(bool isare, bool shorten = false, bool capitalise = false) { //PS stands for Prefix Suffix
		string ret;
		if (shorten) {
			if (isare) { ret = hashave_array[chosen] == (int)Ha.s ? "'s" : "'re"; }
			else       { ret = hashave_array[chosen] == (int)Ha.s ? "'s" : "'ve"; }
		} else {
			if (isare) { ret = hashave_array[chosen] == (int)Ha.s ? "is" : "are"; }
			else       { ret = hashave_array[chosen] == (int)Ha.s ? "has" : "have"; }
		}
		return capitalise ? ret.Capitalize() : ret;
	}
}