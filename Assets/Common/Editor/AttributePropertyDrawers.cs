using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

/*
//[CustomEditor(typeof(TempleIdleGenerator))]
public class TempleIdleGeneratorEditor : Editor {
	public override void OnInspectorGUI() {
		DrawDefaultInspector();

		TempleIdleGenerator tig = (TempleIdleGenerator)target;
		if (GUILayout.Button("TestButton")) {
			Debug.Log("Editor button");
		}
	}
}
*/
/*

[CustomPropertyDrawer(typeof(TempleNameAttribute))]
public class SkillNameDrawer : PropertyDrawer {

	// Draw the property inside the given rect
	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {

		TempleNameAttribute tna  = attribute as TempleNameAttribute;

		var names = ItemDB.GetValues (tna.name).ToList ();

		int index = 0;
		if (names.Contains (property.stringValue)) {
			index = names.FindIndex (s => s == property.stringValue);
		} else {
			index = names.Count ();
			names.Add (property.stringValue);
		}

		int newIndex = EditorGUI.Popup(position, label.text, index, names.ToArray());

		if (newIndex != index) property.stringValue = names[newIndex];

	}
}
*/



//[CustomPropertyDrawer(typeof(EnumLabelAttribute))]
//[CustomPropertyDrawer (typeof (SlotItemFreq))]
//[CustomPropertyDrawer (typeof (RaceSlotItemFreq))]
//[CustomPropertyDrawer (typeof (RaceItemSkillFreq))]
public class LabelEnum : PropertyDrawer {
	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
		// use the default property height, which takes into account the expanded state
		return EditorGUI.GetPropertyHeight(property);
	}

	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
		EditorGUI.BeginProperty (position, label, property);

		// Draw label

		var tempProp = property.Copy();
		property.NextVisible (true);
		int index = property.enumValueIndex;
		label.text = property.enumDisplayNames [index];
		property = tempProp;

		EditorGUI.PropertyField(position, property, label, true);
		EditorGUI.EndProperty ();
	}
}
 
//[CustomPropertyDrawer (typeof (PlayerTag))]
//[CustomPropertyDrawer (typeof (APTag))]
public class LabelTagDrawer : PropertyDrawer {
	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
		// use the default property height, which takes into account the expanded state
		return EditorGUI.GetPropertyHeight(property);
	}

	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
		EditorGUI.BeginProperty (position, label, property);

		// Draw label


		//var tempProp = property.Copy();
		string current = property.FindPropertyRelative ("current").floatValue.ToString("N2");
		string max = property.FindPropertyRelative ("max").floatValue.ToString("N2");


		label.text += " "+ current + "/" + max;
		//property = tempProp;

		EditorGUI.PropertyField(position, property, label, true);
		EditorGUI.EndProperty ();
	}
}


//  //[CustomPropertyDrawer(typeof(EnumLabelAttribute))]
public class DupListElementDrawer : PropertyDrawer {
	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
		// use the default property height, which takes into account the expanded state
		return EditorGUI.GetPropertyHeight(property);
	}

	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
		EditorGUI.BeginProperty (position, label, property);



		var tempProp = property.Copy();
		property.NextVisible (true);
		int index = property.enumValueIndex;
		label.text = property.enumDisplayNames [index];
		property = tempProp;

		EditorGUI.PropertyField(position, property, label, true);
		EditorGUI.EndProperty ();
	}
}



//[CustomPropertyDrawer (typeof (TagSkill))]
public class TempleDrawer : PropertyDrawer {

	public float posX ;
	public float posBase;
	public float posY ;
	public float spacer = 5;
	public float labelSpacer = 2;
	public float height = 16;
	public float newLine = 18;

	public Rect DisplayRect(float width, bool label = false){
		var result = new Rect (posX, posY, width, height);
		posX += width;
		if (label) {
			posX += labelSpacer;
		} else {
			posX += spacer;
		}
		return result;
	}

	public void NewLine(){
		posY += newLine;
		posX = posBase;
	}
}

//[CustomPropertyDrawer (typeof (ItemFreq))]
public class ItemFreqDrawer : PropertyDrawer {

	// Draw the property inside the given rect
	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
		// Using BeginProperty / EndProperty on the parent property means that
		// prefab override logic works on the entire property.
		EditorGUI.BeginProperty (position, label, property);

		// Draw label
		position = EditorGUI.PrefixLabel (position, GUIUtility.GetControlID (FocusType.Passive), label);

		// Don't make child fields be indented
		var indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;

		// Calculate rects
		var amountRect = new Rect (position.x, position.y, 90, position.height);
		var unitRect = new Rect (position.x+95, position.y, 50, position.height);
		var nameRect = new Rect(position.x + 150, position.y, 50, position.height);
		var freq2Rect = new Rect(position.x + 205, position.y, 50, position.height);

		// Draw fields - passs GUIContent.none to each so they are drawn without labels
		EditorGUI.PropertyField(amountRect, property.FindPropertyRelative ("itemName"), GUIContent.none);
		EditorGUI.PropertyField(unitRect, property.FindPropertyRelative ("size"), GUIContent.none);
		EditorGUI.PropertyField(nameRect, property.FindPropertyRelative("frequency"), GUIContent.none);
		EditorGUI.PropertyField(freq2Rect, property.FindPropertyRelative("frequency2"), GUIContent.none);

		// Set indent back to what it was
		EditorGUI.indentLevel = indent;

		EditorGUI.EndProperty ();
	}
}
	
//[CustomPropertyDrawer (typeof (AreaFreq))]
public class AreaFreqDrawer : PropertyDrawer {

	// Draw the property inside the given rect
	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
		// Using BeginProperty / EndProperty on the parent property means that
		// prefab override logic works on the entire property.
		EditorGUI.BeginProperty (position, label, property);

		// Draw label
		position = EditorGUI.PrefixLabel (position, GUIUtility.GetControlID (FocusType.Passive), label);

		// Don't make child fields be indented
		var indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;

		// Calculate rects
		var offenceRect = new Rect (position.x, position.y, 30, position.height);
		var areaRect = new Rect (position.x+35, position.y, 90, position.height);
		var freqRect = new Rect(position.x + 150, position.y, 50, position.height);
		var freq2Rect = new Rect(position.x + 205, position.y, 50, position.height);

		// Draw fields - passs GUIContent.none to each so they are drawn without labels
		EditorGUI.PropertyField(offenceRect, property.FindPropertyRelative ("offence"), GUIContent.none);
		EditorGUI.PropertyField(areaRect, property.FindPropertyRelative ("area"), GUIContent.none);
		EditorGUI.PropertyField(freqRect, property.FindPropertyRelative("frequency"), GUIContent.none);
		EditorGUI.PropertyField(freq2Rect, property.FindPropertyRelative("frequency2"), GUIContent.none);

		// Set indent back to what it was
		EditorGUI.indentLevel = indent;

		EditorGUI.EndProperty ();
	}
}

//[CustomPropertyDrawer (typeof (RealmFreq))]
public class RealmFreqDrawer : PropertyDrawer {

	// Draw the property inside the given rect
	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
		// Using BeginProperty / EndProperty on the parent property means that
		// prefab override logic works on the entire property.
		EditorGUI.BeginProperty (position, label, property);

		// Draw label
		position = EditorGUI.PrefixLabel (position, GUIUtility.GetControlID (FocusType.Passive), label);

		// Don't make child fields be indented
		var indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;

		// Calculate rects
		var offenceRect = new Rect (position.x, position.y, 30, position.height);
		var realmRect = new Rect (position.x+35, position.y, 90, position.height);
		var freqRect = new Rect(position.x + 150, position.y, 50, position.height);
		var freq2Rect = new Rect(position.x + 205, position.y, 50, position.height);

		// Draw fields - passs GUIContent.none to each so they are drawn without labels
		EditorGUI.PropertyField(offenceRect, property.FindPropertyRelative ("offence"), GUIContent.none);
		EditorGUI.PropertyField(realmRect, property.FindPropertyRelative ("realm"), GUIContent.none);
		EditorGUI.PropertyField(freqRect, property.FindPropertyRelative("frequency"), GUIContent.none);
		EditorGUI.PropertyField(freq2Rect, property.FindPropertyRelative("frequency2"), GUIContent.none);

		// Set indent back to what it was
		EditorGUI.indentLevel = indent;

		EditorGUI.EndProperty ();
	}
}


//[CustomPropertyDrawer (typeof (SkillFreq))]
public class SkillFreqDrawer : PropertyDrawer {

	// Draw the property inside the given rect
	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
		// Using BeginProperty / EndProperty on the parent property means that
		// prefab override logic works on the entire property.
		EditorGUI.BeginProperty (position, label, property);

		// Draw label
		position = EditorGUI.PrefixLabel (position, GUIUtility.GetControlID (FocusType.Passive), label);

		// Don't make child fields be indented
		var indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;

		// Calculate rects
		var skillNameRect = new Rect (position.x, position.y, 120, position.height);
		var freqRect = new Rect(position.x + 125, position.y, 50, position.height);
		var freq2Rect = new Rect(position.x + 180, position.y, 50, position.height);

		// Draw fields - passs GUIContent.none to each so they are drawn without labels
		EditorGUI.PropertyField(skillNameRect, property.FindPropertyRelative ("skillName"), GUIContent.none);
		EditorGUI.PropertyField(freqRect, property.FindPropertyRelative("frequency"), GUIContent.none);
		EditorGUI.PropertyField(freq2Rect, property.FindPropertyRelative("frequency2"), GUIContent.none);

		// Set indent back to what it was
		EditorGUI.indentLevel = indent;

		EditorGUI.EndProperty ();
	}
}

//[CustomPropertyDrawer (typeof (ItemQualityAdjust))]
public class ItemQualityAdjustDrawer : PropertyDrawer {

	// Draw the property inside the given rect
	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
		// Using BeginProperty / EndProperty on the parent property means that
		// prefab override logic works on the entire property.
		EditorGUI.BeginProperty (position, label, property);

		// Draw label
		position = EditorGUI.PrefixLabel (position, GUIUtility.GetControlID (FocusType.Passive), label);

		// Don't make child fields be indented
		var indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;

		// Calculate rects
		var itemNameRect = new Rect (position.x, position.y, 120, position.height);
		var qualityAdjustRect = new Rect(position.x + 125, position.y, 50, position.height);
		var qualityAdjustRect2 = new Rect(position.x + 180, position.y, 50, position.height);

		// Draw fields - passs GUIContent.none to each so they are drawn without labels
		EditorGUI.PropertyField(itemNameRect, property.FindPropertyRelative ("itemName"), GUIContent.none);
		EditorGUI.PropertyField(qualityAdjustRect, property.FindPropertyRelative("qualityAdjust"), GUIContent.none);
		EditorGUI.PropertyField(qualityAdjustRect2, property.FindPropertyRelative("qualityAdjust2"), GUIContent.none);

		// Set indent back to what it was
		EditorGUI.indentLevel = indent;

		EditorGUI.EndProperty ();
	}
}

//[CustomPropertyDrawer (typeof (ItemQuantityParam))]
public class ItemQuantityParamDrawer : PropertyDrawer {

	// Draw the property inside the given rect
	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
		// Using BeginProperty / EndProperty on the parent property means that
		// prefab override logic works on the entire property.
		EditorGUI.BeginProperty (position, label, property);

		// Draw label
		position = EditorGUI.PrefixLabel (position, GUIUtility.GetControlID (FocusType.Passive), label);

		// Don't make child fields be indented
		var indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;

		// Calculate rects
		var posX = position.x;
		int spacer = 5;
		int width = 90;
		var ItemName = new Rect (posX, position.y, width, position.height);
		posX += width+spacer;
		width = 40;
		var mult = new Rect (posX, position.y, width, position.height);
		posX += width+spacer;

		var add = new Rect (posX, position.y, position.width-150, position.height);

		// Draw fields - passs GUIContent.none to each so they are drawn without labels
		EditorGUI.PropertyField(ItemName, property.FindPropertyRelative ("itemName"), GUIContent.none);
		EditorGUI.PropertyField(mult, property.FindPropertyRelative ("mult"), GUIContent.none);
		EditorGUI.PropertyField(add, property.FindPropertyRelative ("add"), GUIContent.none);

		// Set indent back to what it was
		EditorGUI.indentLevel = indent;

		EditorGUI.EndProperty ();
	}
}

//[CustomPropertyDrawer (typeof (TagSkill))]
public class TagSkillDrawer : TempleDrawer {



	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
		// use the default property height, which takes into account the expanded state
		return 80;
		//return EditorGUI.GetPropertyHeight(property);
	}
		
	// Draw the property inside the given rect
	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {


		EditorGUI.BeginProperty (position, label, property);

		position = EditorGUI.PrefixLabel (position, GUIUtility.GetControlID (FocusType.Passive), label);
		var indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;

		// Calculate rects
		posX = position.x;
		posBase = position.x;
		posY = position.y;
		spacer = 5;
		labelSpacer = 2;
		height = 16;
		newLine = 18;
	
		var tagLabel =DisplayRect (30, true);
		var tag = DisplayRect (120);
		NewLine ();
		var tagGroup = DisplayRect (60, true);
		var tagUse = DisplayRect (60);
		var tagXP = DisplayRect (60);
		NewLine ();
		var levelLabel = DisplayRect (20, true);
		var level = DisplayRect (40);
		var modifierLabel = DisplayRect (26, true);
		var modifier = DisplayRect (40);
		var tagRealm = DisplayRect (60);
		NewLine ();
		var nameLabel = DisplayRect (32, true);
		var name = DisplayRect (80);
		var currentLabel = DisplayRect (30, true);
		var current = DisplayRect (40);
		var maxLabel = DisplayRect (26, true);
		var max = DisplayRect (40);
		//newLine
		posY += newLine/2;
		posX = position.x;

		var separatorLabel = DisplayRect (150, true);

		// Draw fields - passs GUIContent.none to each so they are drawn without labels
		GUI.Label(tagLabel,"Tag");
		EditorGUI.PropertyField(tag,property.FindPropertyRelative ("tag"), GUIContent.none);

		EditorGUI.PropertyField(tagGroup,property.FindPropertyRelative ("tagGroup"), GUIContent.none);
		EditorGUI.PropertyField(tagUse,property.FindPropertyRelative ("tagUse"),GUIContent.none);
		EditorGUI.PropertyField(tagXP,property.FindPropertyRelative ("tagXP"), GUIContent.none);
		EditorGUI.PropertyField(tagRealm,property.FindPropertyRelative ("tagRealm"),GUIContent.none);

		GUI.Label(levelLabel,"Lvl");
		EditorGUI.PropertyField(level,property.FindPropertyRelative ("level"), GUIContent.none);
		GUI.Label(modifierLabel,"Mod");
		EditorGUI.PropertyField(modifier,property.FindPropertyRelative ("modifier"),GUIContent.none);

		GUI.Label(nameLabel,"name");
		EditorGUI.PropertyField(name,property.FindPropertyRelative ("name"), GUIContent.none);
		GUI.Label(currentLabel,"Curr");
		EditorGUI.PropertyField(current,property.FindPropertyRelative ("current"),GUIContent.none);
		GUI.Label(maxLabel,"Max");
		EditorGUI.PropertyField(max,property.FindPropertyRelative ("max"), GUIContent.none);

		GUI.Label(separatorLabel,"_______________________________________________");

		// Set indent back to what it was
		EditorGUI.indentLevel = indent;

		EditorGUI.EndProperty ();

	}
}
		
//[CustomPropertyDrawer (typeof (EffectAttack))]
public class EffectAttackDrawer : TempleDrawer {

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
		// use the default property height, which takes into account the expanded state
		return 20;
		//return EditorGUI.GetPropertyHeight(property);
	}

	// Draw the property inside the given rect
	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {


		EditorGUI.BeginProperty (position, label, property);

		position = EditorGUI.PrefixLabel (position, GUIUtility.GetControlID (FocusType.Passive), label);
		var indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;

		posX = position.x;
		posBase = position.x;
		posY = position.y;
		spacer = 5;
		labelSpacer = 2;
		height = 16;
		newLine = 18;

		var tagRealm = DisplayRect (60);
		var range = DisplayRect (60);

		var damageMultiplierLabel = DisplayRect (40, true);
		var damageMultiplier = DisplayRect (30);

		// Draw fields - passs GUIContent.none to each so they are drawn without labels

		EditorGUI.PropertyField(tagRealm,property.FindPropertyRelative ("tagRealm"), GUIContent.none);
		EditorGUI.PropertyField(range,property.FindPropertyRelative ("range"), GUIContent.none);
		GUI.Label(damageMultiplierLabel,"D Mult");
		EditorGUI.PropertyField(damageMultiplier,property.FindPropertyRelative ("damageMultiplier"), GUIContent.none);


		// Set indent back to what it was
		EditorGUI.indentLevel = indent;

		EditorGUI.EndProperty ();

	}
}



//not using EffectEnhanceDrawer because tagSkill list does not display well
//[CustomPropertyDrawer (typeof (EffectEnhance))]
public class EffectEnhanceDrawer : TempleDrawer {

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
		// use the default property height, which takes into account the expanded state
		//return 20;
		return EditorGUI.GetPropertyHeight(property)-35;
	}

	// Draw the property inside the given rect
	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
		EditorGUI.BeginProperty (position, label, property);
		var origX = position.x;

		position = EditorGUI.PrefixLabel (position, GUIUtility.GetControlID (FocusType.Passive), label);
		var indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;

		posX = position.x;
		posBase = position.x;
		posY = position.y;
		spacer = 5;
		labelSpacer = 2;
		height = 16;
		newLine = 18;

		var name = DisplayRect (90);
		var enhanceType = DisplayRect (60);
		NewLine ();
		posX = origX +60;
		var tagSkills = DisplayRect(position.width);

		// Draw fields - passs GUIContent.none to each so they are drawn without labels

		EditorGUI.PropertyField(name,property.FindPropertyRelative ("name"), GUIContent.none);
		EditorGUI.PropertyField(enhanceType,property.FindPropertyRelative ("enhanceType"), GUIContent.none);
		EditorGUI.PropertyField(tagSkills,property.FindPropertyRelative ("tagSkills"),true);

		// Set indent back to what it was
		EditorGUI.indentLevel = indent;

		EditorGUI.EndProperty ();

	}
}
//[CustomPropertyDrawer (typeof (TokenString))]
public class TokenStringDrawer : TempleDrawer {

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
		// use the default property height, which takes into account the expanded state
		return 58;
		//return EditorGUI.GetPropertyHeight(property);
	}

	// Draw the property inside the given rect
	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
		EditorGUI.BeginProperty (position, label, property);

		position = EditorGUI.PrefixLabel (position, GUIUtility.GetControlID (FocusType.Passive), label);
		var indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;

		posX = position.x;
		posBase = position.x;
		posY = position.y;
		spacer = 5;
		labelSpacer = 2;
		height = 16;
		newLine = 18;

		var name = DisplayRect (120);
		var multiplier = DisplayRect(40);
		NewLine ();
		posX = posBase ;
		var tokenString = DisplayRect (position.width);
		NewLine ();
		posX = posBase ;
		var tagUse = DisplayRect (60);
		var tagGroup = DisplayRect (60);
		var tagXp = DisplayRect (60);

		// Draw fields - passs GUIContent.none to each so they are drawn without labels

		EditorGUI.PropertyField(name,property.FindPropertyRelative ("name"), GUIContent.none);
		EditorGUI.PropertyField(multiplier,property.FindPropertyRelative ("multiplier"),GUIContent.none);
		EditorGUI.PropertyField(tokenString,property.FindPropertyRelative ("tokenString"), GUIContent.none);
		EditorGUI.PropertyField(tagUse,property.FindPropertyRelative ("tagUse"), GUIContent.none);
		EditorGUI.PropertyField(tagGroup,property.FindPropertyRelative ("tagGroup"),GUIContent.none);
		EditorGUI.PropertyField(tagXp,property.FindPropertyRelative ("tagXp"), GUIContent.none);


		// Set indent back to what it was
		EditorGUI.indentLevel = indent;

		EditorGUI.EndProperty ();

	}
}
	
//[CustomPropertyDrawer (typeof (ActionTime))]
public class ActionTimeDrawer : TempleDrawer {

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
		// use the default property height, which takes into account the expanded state
		return 19;
		//return EditorGUI.GetPropertyHeight(property);
	}

	// Draw the property inside the given rect
	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
		EditorGUI.BeginProperty (position, label, property);

		position = EditorGUI.PrefixLabel (position, GUIUtility.GetControlID (FocusType.Passive), label);

		var indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;

		posX = position.x;
		posBase = position.x;
		posY = position.y;
		spacer = 5;
		labelSpacer = 2;
		height = 16;
		newLine = 18;

		var action = DisplayRect (120);
		var time = DisplayRect(40);

		// Draw fields - passs GUIContent.none to each so they are drawn without labels

		EditorGUI.PropertyField(action,property.FindPropertyRelative ("action"), GUIContent.none);
		EditorGUI.PropertyField(time,property.FindPropertyRelative ("time"),GUIContent.none);


		// Set indent back to what it was
		EditorGUI.indentLevel = indent;

		EditorGUI.EndProperty ();

	}
}


//[CustomPropertyDrawer (typeof (SOAttribute))]
public class SODrawer : PropertyDrawer {

	//this might be needed not sure

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
		var obj = (new SerializedObject (property.objectReferenceValue));
		var so = obj.GetIterator();
		so.NextVisible(true);
		so.NextVisible(true);

		return EditorGUI.GetPropertyHeight (so) ;
	}

	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {

		EditorGUI.PropertyField(position, property, label, true);
		var obj = (new SerializedObject (property.objectReferenceValue));
		obj.Update ();
		var so = obj.GetIterator();

		so.NextVisible(true);

		so.NextVisible(true);
		EditorGUI.PropertyField(position, so, label, true);
		obj.ApplyModifiedProperties();
	}
}

