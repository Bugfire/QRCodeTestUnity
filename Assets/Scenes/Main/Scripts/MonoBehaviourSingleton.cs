using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MainBehaviours
{

    public class MonoBehaviourSingleton<T> : MonoBehaviour
        where T : MonoBehaviourSingleton<T>
    {
        static T _instance = null;

        static public T Instance { get { return _instance; } }

        void Awake()
        {
            if (_instance != null)
            {
                Debug.LogError("Already exists");
            }
            _instance = this as T;
        }
    }

}