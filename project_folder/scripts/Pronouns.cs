using Godot;
using System;

public partial class Pronouns : Node 
{
    private string[,] variants = new string[12,5] {
		{"he",   "him",   "his",   "his",   "himself"},
		{"she",  "her",   "her",   "hers",  "herself"},
		{"they", "them",  "their", "theirs","themself"}, 
		{"it",   "it",    "its",   "its",   "itself"},
		{"ve",   "ver",   "vis",   "vis",   "verself"},
		{"xe",   "xem",   "xyr",   "xyrs",  "xemself"},
		{"ey",   "em",    "eir",   "eirs",  "eirself"},
		{"He",   "Him",   "His",   "His",   "Himself"},
		{"shkle","shkler","shkler","shklis","shklimself"},
		{"", "", "", "", ""}, {"", "", "", "", ""}, {"", "", "", "", ""} //Empty pronouns for player's to make their own
	};
    enum Ha {s,ve}; //Determinds if the pronoun uses has or have (I'm very clever)
	private Ha[] hashave_array = new Ha[12] { Ha.s, Ha.s, Ha.ve, Ha.s, Ha.s, Ha.s, Ha.ve, Ha.s, Ha.s, /*The last 3 are for customs*/ Ha.s, Ha.s, Ha.s };
	private int chosen = 0;
	public int ChosenPronoun { get { return chosen; } set { chosen = value; } }
	public string X(int form, bool capitalise = false) {
		string ret = variants[chosen, form];
		return capitalise ? ret.Capitalize() : ret;
	}
	public string PS(string isare, bool shorten = false, bool capitalise = false) { //PS stands for Prefix Suffix
		string ret;
		if (shorten) {
			if      (isare == "are")  { ret = hashave_array[chosen] == (int)Ha.s ? "'s" : "'re"; }
			else if (isare == "have") { ret = hashave_array[chosen] == (int)Ha.s ? "'s" : "'ve"; }
			else ret = "[INVALID ISARE VALUE: '" + isare + "']";
		} else {
			if      (isare == "are")  { ret = hashave_array[chosen] == (int)Ha.s ? "is" : "are"; }
			else if (isare == "have") { ret = hashave_array[chosen] == (int)Ha.s ? "has" : "have"; }
			else ret = "[INVALID ISARE VALUE: '" + isare + "']";
		}
		return capitalise ? ret.Capitalize() : ret;
	}

	//Functions below change custom pronouns
	//PLEASE NOTE: The following functions will close if the n parameter does not equal the indexes of the custom pronoun places
	public void ChangeCustom(int n, int i, string np) { 
		if (n<9 || n>11) return; 
		variants[n, i] = np; 
	}
	public void ChangeCustom(int n, string np0, string np1, string np2, string np3, string np4) {
		//This overload should accept any iterable and use that to set new neo
		if (n<9 || n>11) return; 
		variants[n, 0] = np0; 
		variants[n, 1] = np1; 
		variants[n, 2] = np2; 
		variants[n, 3] = np3; 
		variants[n, 4] = np4; 
	}
	public void ChangeCustomHas(int n, bool ishas) { 
		if (n<9 || n>11) return;
		hashave_array[n] = ishas ? Ha.s : Ha.ve;
	}
}