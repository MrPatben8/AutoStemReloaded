using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Fader : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler, IPointerExitHandler
{
	private Vector2 m_localPos, m_screenPos, m_startPos;
	private Camera m_eventCamera;
	private bool isPointerDown, isPointerReleased, pointerAbove;

	public Vector2 valueRange = new Vector2(0, 1);
	public float value = 0.5f;
	public float defaultValue;
	public float sensitivity = 1;
	public Image fill;
	public bool fillCentered;

	public float fillRange = 1.0f;
	public int decimalPlaces;

	public Text valueDisplay;
	public string unit = "";
	public string label = "";
	// Start is called before the first frame update
	void Awake()
    {
		valueDisplay.text = label;
    }

    // Update is called once per frame
    void Update()
    {
		if(fillCentered)
		{
			fill.fillOrigin = 2;
			float fillAmount = Mathf.InverseLerp(valueRange.x, valueRange.y, value);
			if(fillAmount > 0.5)
			{
				fill.fillClockwise = true;
				fill.fillAmount = Mathf.Lerp(0.0f, 0.5f * fillRange, Mathf.InverseLerp(0.5f, 1.0f, fillAmount));
			} else
			{
				fill.fillClockwise = false;
				fill.fillAmount = Mathf.Lerp(0.5f * fillRange, 0.0f, Mathf.InverseLerp(0.0f, 0.5f, fillAmount));
			}
		} else
		{
			fill.fillOrigin = 0;
			fill.fillClockwise = true;
			fill.fillAmount = Mathf.Lerp((1.0f-fillRange)/2.0f , fillRange + ((1.0f - fillRange) / 2.0f), Mathf.InverseLerp(valueRange.x, valueRange.y, value));
		}
		
		if(label == "")
		{
			valueDisplay.text = "" + value + " " + unit;
		} else
		{
			if(pointerAbove || isPointerDown)
			{
				valueDisplay.text = "" + (Mathf.Round(value * (Mathf.Pow(10, decimalPlaces))) / Mathf.Pow(10, decimalPlaces)) + " " + unit;
			} else
			{
				valueDisplay.text = label;
			}
		}
    }

	float startValue;
	public void OnPointerExit(PointerEventData eventData)
	{
		pointerAbove = false;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		pointerAbove = true;
		m_screenPos = eventData.position;
		m_eventCamera = eventData.enterEventCamera;
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if(Input.GetMouseButton(1))
		{
			value = defaultValue;
		} else
		{
			m_screenPos = eventData.position;
			m_startPos = eventData.position;
			m_eventCamera = eventData.enterEventCamera;
			isPointerDown = true;
			startValue = value;
		}
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		m_screenPos = Vector2.zero;
		isPointerDown = false;
		isPointerReleased = true;
	}

	public void OnDrag(PointerEventData eventData)
	{
		m_screenPos = eventData.position;
		value = Mathf.Clamp(startValue + (m_screenPos.y - m_startPos.y) * (sensitivity*0.00015f) * (valueRange.y - valueRange.x), valueRange.x, valueRange.y);
	}
}
