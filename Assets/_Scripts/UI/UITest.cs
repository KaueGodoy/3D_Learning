using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UITest : MonoBehaviour
{
    [Header("Debugging")]
    [SerializeField] Logger logger;

    public void OnPressed()
    {
        logger.Log("Pressed", this);
    }
}
