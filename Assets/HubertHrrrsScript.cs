using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;  

public class HubertHrrrsScript : MonoBehaviour {

	//globals
    public KMBombInfo Bomb;
    public KMAudio Audio;

	//Logging
	private static int moduleIdCounter = 1;
    private int moduleId;
	private bool moduleSolved = false; 

	//Selectables
	public KMSelectable[] buttons; 
	public KMSelectable villager; 

	//Objects
	public GameObject pivot; 
	public GameObject vilHL; 
	public GameObject villagerHand; 

	//Sprites
	public Sprite[] heads; 
	public Sprite[] buttonSprites;
	public Sprite coal;
	public Sprite apple; 

	//Variables
	private string[][] sounds = {
		new string[]{"A1","A2","A3"},
		new string[]{"D1","D2","D3"},
		new string[]{"H1","H2","H3"},
		new string[]{"I1","I2","I3"},
		new string[]{"T1","T2","T3"}
	};
	private string[] names = {"Accept", "Deny", "Hurt", "Idle", "Trade"}; 
	private string[] buttonNames = {"Diamond","Emerald","Gold","Iron","Redstone"}; 
	private List<int> inds = new List<int>(); 
	private int[] assignments;  
	private int[] types = new int[5]; 
	private bool submission; 
	private bool cond1; 
	private bool started = false; 
	private int stage;
	private int pressCount = 0; 
	private int[] sequence = new int[5]; 
	private int row; 
	private string sequenceName; 
	Vector3 start;
	Vector3 end; 
	float velocity = 15f; 
	bool animating = false;

	//Tables

	private int[][] table1 = { //Dia Eme Gol Iro Red
		new int[]{0, 1, 4, 3, 2},
		new int[]{4, 3, 0, 2, 1},
		new int[]{3, 0, 2, 1, 4}
	};
	private int[][] table2 = {
		new int[]{2, 4, 3, 0, 1},
		new int[]{0, 2, 1, 4, 3},
		new int[]{1, 3, 2, 4, 0}
	}; 

	void Awake () {
		moduleId = moduleIdCounter++;
		villager.OnInteract += delegate () {VillagerPress(); return false;};
		for (int i = 0; i < buttons.Length; i++){
			int j = i; 
			buttons[i].OnInteract += delegate(){ButtonPress(j); return false;}; 
		}
		start = villager.gameObject.transform.localScale; 
		end = start + new Vector3(0.02f, 0.02f, 0f); 
	}

	void Start () {
		villagerHand.gameObject.SetActive(false); 
		vilHL.transform.localScale = new Vector3(.7f,.1f,1f); 
		if (!started){
			if (Bomb.GetSerialNumberLetters().Any(x => x == 'V' || x == 'L' || x == 'A' || x == 'G' || x == 'R') || Bomb.GetSerialNumberNumbers().Any(x => x == 1 || x == 3)){cond1 = true;}
			else {cond1 = false;}
			if (cond1){
				if (Bomb.GetBatteryCount(Battery.D) < Bomb.GetBatteryCount(Battery.AA)){row = 0;}
				else if (Bomb.GetBatteryCount(Battery.D) > Bomb.GetBatteryCount(Battery.AA)){row = 1;}
				else {row = 2;}
			}
			else{
				if (Bomb.GetOnIndicators().Count() > Bomb.GetOffIndicators().Count()){row = 0;}
				else if (Bomb.GetOnIndicators().Count() < Bomb.GetOffIndicators().Count()){row = 1;}
				else{row = 2;}
			}
			Debug.LogFormat("[Simon Hrrrs #{0}] Using the {1} table, row #{2}.", moduleId, cond1 ? "1st" : "2nd", row+1);
		}
		started = true; 
		submission = false; 
		villager.GetComponent<SpriteRenderer>().sprite = heads[0]; 
		for (int i = 0; i < buttons.Length; i++){
			buttons[i].GetComponent<SpriteRenderer>().sprite = buttonSprites[i]; 
		}
		stage = 1; 
		sequenceName = ""; 
		inds.Clear(); 
		AssignSounds(); 
	}
	
	void Update () {
		if (!moduleSolved){
			pivot.transform.Rotate(Vector3.up*velocity*Time.deltaTime); 
		}
	}

	void VillagerPress(){
		if (submission){return;}
		villager.AddInteractionPunch(1.0f); 
		StartCoroutine(Sequence());
		submission = true; 
		vilHL.transform.localScale = new Vector3(0f,0f,0f); 
	}

	IEnumerator Sequence(){ 
		pressCount = 0; 
		float elapsed = 0f; 
		float duration = 0.75f; 
		while (elapsed < duration){
			yield return null;
			elapsed += Time.deltaTime; 
		}
		float elapsedS = 0f;
		float durationS = 0.75f; 
		float elapsedL = 0f; 
		float durationL = 1.75f; 
		sequence[stage-1] = UnityEngine.Random.Range(0, 5); 
		sequenceName += (stage == 1 ? "" : ", ") + buttonNames[cond1 ? table1[row][sequence[stage-1]] : table2[row][sequence[stage-1]] ]; 
		Debug.LogFormat("[Simon Hrrrs #{0}] Added the {1} sound.", moduleId, buttonNames[sequence[stage-1]]);
		Debug.LogFormat("[Simon Hrrrs #{0}] Expecting: {1}.", moduleId, sequenceName);
		while (true){
			for (int i = 0; i < stage; i++){
				elapsedS = 0f; 
				while (elapsedS < durationS){
					yield return null;
					elapsedS += Time.deltaTime; 
				}
				Audio.PlaySoundAtTransform(sounds[assignments[sequence[i]]][types[sequence[i]]], transform); 
			}
			elapsedL = 0f; 
			while (elapsedL < durationL){
				yield return null;
				elapsedL += Time.deltaTime; 
			}
		}
		
	}

	void ButtonPress(int i){
		if (animating || moduleSolved){return;}
		buttons[i].AddInteractionPunch(0.7f); 
		if (submission){	
			StopAllCoroutines();
			if (buttonNames[i] == buttonNames[cond1 ? table1[row][sequence[pressCount]] : table2[row][sequence[pressCount]] ]){
				Audio.PlaySoundAtTransform(sounds[assignments[i]][types[i]], transform);
				Debug.LogFormat("[Simon Hrrrs #{0}] Correctly pressed {1}.", moduleId, buttonNames[i]);
				pressCount++; 
				if (pressCount == stage){
					if (stage == 5){
						StartCoroutine(Solve()); 
					}
					else{
						stage++; 
						StartCoroutine(Sequence()); 
					}
				}
			}
			else{
				StartCoroutine(Strike()); 
				Debug.LogFormat("[Simon Hrrrs #{0}] Incorrectly pressed {1}.", moduleId, buttonNames[i]);
			}
		}
		else{
			Audio.PlaySoundAtTransform(sounds[assignments[i]][types[i]], transform);
		}
	}

	IEnumerator Strike(){
		animating = true; 
		float elapsed; 
		float duration = 0.2f; 
		float duration2 = 0.05f; 
		for (int i = 0; i < buttons.Length; i++){
			Audio.PlaySoundAtTransform(sounds[2][UnityEngine.Random.Range(0,3)], transform);
			elapsed = 0f; 
			while (elapsed < duration2){
				yield return null;
				elapsed += Time.deltaTime; 
			}
			buttons[i].GetComponent<SpriteRenderer>().sprite = coal; 
			elapsed = 0f; 
			while (elapsed < duration){
				yield return null;
				elapsed += Time.deltaTime; 
			}
		}
		duration = 0.15f; 
		elapsed = 0f;  
		while (elapsed < duration){
			yield return null;
			villager.gameObject.transform.localScale = Vector3.Lerp(start, end, elapsed/duration); 
			elapsed += Time.deltaTime; 
		} 
		elapsed = 0f; 
		while (elapsed < duration){
			yield return null;
			villager.gameObject.transform.localScale = Vector3.Lerp(end, start, elapsed/duration);
			elapsed += Time.deltaTime; 
		}
		villager.GetComponent<SpriteRenderer>().sprite = heads[1];
		Audio.PlaySoundAtTransform("Death", transform);
		GetComponent<KMBombModule>().HandleStrike();
		elapsed = 0f; 
		duration = 1.25f; 
		while (elapsed < duration){
			yield return null;
			elapsed += Time.deltaTime; 
		}
		animating = false;
		Start();
		yield break; 
	}

	IEnumerator Solve(){
		animating = true; 
		float elapsed; 
		float duration = 0.15f; 
		float duration2 = 0.1f; 
		for (int i = 0; i < buttons.Length; i++){
			Audio.PlaySoundAtTransform("XP", transform);
			elapsed = 0f; 
			while (elapsed < duration2){
				yield return null;
				elapsed += Time.deltaTime; 
			}
			buttons[i].GetComponent<SpriteRenderer>().sprite = apple; 
			elapsed = 0f; 
			while (elapsed < duration){
				yield return null;
				elapsed += Time.deltaTime; 
			}
		}
		duration = 0.5f; 
		elapsed = 0f; 
		while (elapsed < duration){
			yield return null;
			elapsed += Time.deltaTime; 
		}
		Audio.PlaySoundAtTransform("A2", transform);
		
		GetComponent<KMBombModule>().HandlePass();
		elapsed = 0f;
		duration = 3f; 
		float vi = velocity; 
		while (elapsed < duration){
			yield return null; 
			velocity = vi + 30*(elapsed/duration)*vi; 
			elapsed += Time.deltaTime; 
		}
		Audio.PlaySoundAtTransform("LevelUp", transform);
		for (int i = 0; i < buttons.Length; i++){
			buttons[i].gameObject.SetActive(false); 
		}
		villagerHand.gameObject.SetActive(true); 
		animating = false; 
		moduleSolved = true; 
		yield break; 
	}

	void AssignSounds(){
		for (int j = 0; j < buttons.Length; j++){
			int i = UnityEngine.Random.Range(0, 5);
			int i2 = UnityEngine.Random.Range(0, 3); 
			while (inds.Contains(i)){
				i = UnityEngine.Random.Range(0, 5); 
			}
			inds.Add(i);
			types[j] = i2; 
		}
		assignments = inds.ToArray(); //what each button plays in order
		Debug.LogFormat("[Simon Hrrrs #{0}] Diamond plays the {1} sound.", moduleId, names[assignments[0]]); 
		Debug.LogFormat("[Simon Hrrrs #{0}] Emerald plays the {1} sound.", moduleId, names[assignments[1]]);
		Debug.LogFormat("[Simon Hrrrs #{0}] Gold plays the {1} sound.", moduleId, names[assignments[2]]);
		Debug.LogFormat("[Simon Hrrrs #{0}] Iron plays the {1} sound.", moduleId, names[assignments[3]]);
		Debug.LogFormat("[Simon Hrrrs #{0}] Redstone plays the {1} sound.", moduleId, names[assignments[4]]);
	}
}
