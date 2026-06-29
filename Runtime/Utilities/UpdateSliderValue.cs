    using System.Collections;
    using UnityEngine;
    using UnityEngine.UI;

    public class UpdateSliderValue
    {
        public static void SetValueInCoroutine(float value, Slider slider, MonoBehaviour context)
        {
            context.StartCoroutine(SetValue(value, slider));
        }
        private static IEnumerator SetValue(float value, Slider slider)
        {
            slider.value = value;
            yield return new WaitForSeconds(0.1f);
        }
    }