using UnityEngine;
using System.Collections;

public class Canvas : MonoBehaviour {

	Texture2D canvas;
	public Texture2D logo;
	public float updateFrequency;
	float nextUpdate;
	bool didChange;

	// Use this for initialization
	void Start () {

		canvas = logo;
		canvas.filterMode = FilterMode.Point;
		didChange = false;

	}

	// Update is called once per frame
	void Update () {

		if(nextUpdate < Time.time)
		{
			if(didChange) {
				canvas.Apply();
				didChange = false;
			}
			nextUpdate = updateFrequency + Time.time;
		}

	}

	public void Cleanup () {

		canvas = new Texture2D(96, 64);
		canvas.filterMode = FilterMode.Point;

		Color cleanColor = new Color(0.98f, 0.98f, 0.98f);

		for(int y = 0; y < 64; y++) {
			for(int x = 0; x < 96; x++) {
				float mod = Random.Range(0.0f, 0.02f);
				canvas.SetPixel(x, y, Color.white);
			}
		}

		canvas.Apply();

	}

	public void SetPixel (int x1, int y1, int x2, int y2, float r, float g, float b) {

		Color color = new Color(r,g,b);

		Vector2 from = new Vector2(x1, y1);
		Vector2 to = new Vector2(x2, y2);
		Vector2 dir = to - from;
		if(x1 == x2 && y1 == y2) {
			canvas.SetPixel(Mathf.RoundToInt(from.x), Mathf.RoundToInt(from.y), color);
			didChange = true;
			return;
		}

		dir.Normalize();
		dir *= 0.25f;

		while(Vector2.Distance(from, to) > 0.5f) {

			from += dir;
			canvas.SetPixel(Mathf.RoundToInt(from.x), Mathf.RoundToInt(from.y), color);

		}

		didChange = true;

	}

	void OnGUI () {

		GUI.DrawTexture(new Rect(0, 0, Screen.height * (96.0f / 64.0f), Screen.height), canvas);

	}
}
