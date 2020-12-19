using UnityEngine;
using UnityEngine.Events;

public class InteractEvents : MonoBehaviour {

    private ExamineManager examine;

    public enum Type { InteractCall, Animation, Event }
    public enum Repeat { Once, MoreTimes, OnOff }

    public Type InteractType = Type.InteractCall;
    public Repeat RepeatMode = Repeat.MoreTimes;

    [Space(7)]
    public GameObject InteractObject;
    public string InteractCall = "UseObject";

    [Header("Animation")]
    public string AnimationName;
    public float AnimationSpeed = 1.0f;

    [Header("Event")]
    public UnityEvent InteractEvent;
    public UnityEvent InteractBackEvent;

    [Header("Other")]
    public bool putDownExamine;

    [Header("Sound")]
    public AudioClip InteractSound;
    public float InteractVolume = 1f;

    private bool isInteracted;

    void Awake()
    {
        if (putDownExamine)
        {
            examine = ExamineManager.Instance;
        }
    }

    void Start()
    {
        if (!InteractObject)
        {
            InteractObject = gameObject;
        }
    }

    public void Interact()
    {
        UseObject();
    }

    public void UseObject()
    {
        if(InteractType == Type.InteractCall && InteractObject)
        {
            InteractObject.SendMessage(InteractCall, SendMessageOptions.DontRequireReceiver);
        }
        else if(InteractType == Type.Animation && InteractObject)
        {
            if(RepeatMode == Repeat.Once)
            {
                if (!isInteracted)
                {
                    InteractObject.GetComponent<Animation>()[AnimationName].speed = AnimationSpeed;
                    InteractObject.GetComponent<Animation>().Play(AnimationName);
                    if (InteractSound) { AudioSource.PlayClipAtPoint(InteractSound, transform.position, InteractVolume); }
                    isInteracted = true;
                }
            }
            else
            {
                if (!InteractObject.GetComponent<Animation>().isPlaying)
                {
                    InteractObject.GetComponent<Animation>()[AnimationName].speed = AnimationSpeed;
                    InteractObject.GetComponent<Animation>().Play(AnimationName);
                    if (InteractSound) { AudioSource.PlayClipAtPoint(InteractSound, transform.position, InteractVolume); }
                }
            }
        }
        else if (InteractType == Type.Event)
        {
            if (RepeatMode == Repeat.Once)
            {
                if (!isInteracted)
                {
                    InteractEvent?.Invoke();
                    isInteracted = true;
                }
            }
            else if (RepeatMode == Repeat.MoreTimes)
            {
                InteractEvent?.Invoke();
            }
            else if (RepeatMode == Repeat.OnOff)
            {
                if (!isInteracted)
                {
                    InteractEvent?.Invoke();
                    isInteracted = true;
                }
                else
                {
                    InteractBackEvent?.Invoke();
                    isInteracted = false;
                }
            }

            if (InteractSound) { AudioSource.PlayClipAtPoint(InteractSound, transform.position, InteractVolume); }
        }

        if (putDownExamine && examine)
        {
            examine.CancelExamine();
        }
    }
}
