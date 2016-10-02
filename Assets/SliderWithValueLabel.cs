using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class SliderWithValueLabel : MonoBehaviour {

    public Text ValueLabel;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void setValueLabelText(float text)
    {
        this.ValueLabel.text = text.ToString();
    }
}
