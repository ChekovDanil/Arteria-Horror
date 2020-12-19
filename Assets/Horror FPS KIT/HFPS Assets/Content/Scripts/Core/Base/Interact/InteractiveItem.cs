/*
 * InteractiveItem.cs - by ThunderWire Studio
 * ver. 1.0
*/

using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using ThunderWire.Utility;

/// <summary>
/// Script for defining Interactive Items
/// </summary>
public class InteractiveItem : MonoBehaviour, ISaveable {

    [System.Serializable]
    public class MessageTip
    {
        public string InputString;
        public string KeyMessage;
    }

    private AudioSource audioSource;

    public enum Type { OnlyExamine, GenericItem, InventoryItem, ArmsItem, BackpackExpand, InteractObject }
    public enum ExamineType { None, Object, AdvancedObject, Paper }
    public enum ExamineRotate { None, Horizontal, Vertical, Both }
    public enum MessageType { None, Hint, PickupHint, Message, ItemName }
    public enum DisableType { Disable, Destroy, None }

    public Type ItemType = Type.GenericItem;
    public ExamineType examineType = ExamineType.None;
    public ExamineRotate examineRotate = ExamineRotate.Both;
    public MessageType messageType = MessageType.None;
    public DisableType disableType = DisableType.Disable;

    public string ItemName;
    public string Message;
    public float MessageTime = 3f;

    public MessageTip[] MessageTips;

    public AudioClip PickupSound;
    public AudioClip ExamineSound;

    [Range(0, 1)] public float Volume = 1f;
    public int Amount = 1;

    public bool pickupSwitch;
    public bool examineCollect;
    public bool enableCursor;
    public bool showItemName;
    public bool autoShortcut;
    public bool floatingIconEnabled = true;

    public int WeaponID;
    public int InventoryID;
    public int BackpackExpand;

    public float ExamineDistance;
    public bool faceToCamera = false;

    [Tooltip("Colliders which will be disabled when object will be examined.")]
    public Collider[] CollidersDisable;
    [Tooltip("Colliders which will be enabled when object will be examined.")]
    public Collider[] CollidersEnable;

    [Multiline]
    public string paperReadText;
    public int textSize;

    public Vector3 faceRotation;
    public bool isExamined;

    public CustomItemData customData;
    public List<ItemHashtable> itemHashtables = new List<ItemHashtable>();

    public Vector3 lastFloorPosition;
    private string storedPath;

    void Awake()
    {
        CreateCustomData(itemHashtables);
    }

    void Start()
    {
        audioSource = ScriptManager.Instance.SoundEffects;
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (!collision.collider.isTrigger && collision.collider.tag != "Player")
        {
            lastFloorPosition = transform.position;
        }
    }

    public void CreateCustomData(List<ItemHashtable> hashtables)
    {
        Dictionary<string, string> data = new Dictionary<string, string>();

        if (hashtables.Count > 0)
        {
            foreach (var item in hashtables)
            {
                data.Add(item.Key, item.Value);
            }
        }

        if (ItemType == Type.InventoryItem || ItemType == Type.ArmsItem)
        {
            storedPath = gameObject.GameObjectPath();
            data.Add("object_path", storedPath);
            data.Add("object_scene", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        customData = new CustomItemData(data);
    }

    void FixedUpdate()
    {
        if (ItemType == Type.InventoryItem || ItemType == Type.ArmsItem)
        {
            if(storedPath != gameObject.GameObjectPath())
            {
                storedPath = gameObject.GameObjectPath();
                customData.dataDictionary["object_path"] = storedPath;
            }
        }
    }

    public void UseObject()
    {
        if (ItemType == Type.OnlyExamine) return;

        if (PickupSound)
        {
            audioSource.clip = PickupSound;
            audioSource.volume = Volume;
            audioSource.Play();
        }

        if (GetComponent<ItemEvent>())
        {
            GetComponent<ItemEvent>().DoEvent();
        }

        if (GetComponent<TriggerObjective>())
        {
            GetComponent<TriggerObjective>().OnTrigger();
        }

        SaveGameHandler.Instance.RemoveSaveableObject(gameObject, false, false);

        if (disableType == DisableType.Disable)
        {
            DisableObject(false);
        }
        else if(disableType == DisableType.Destroy)
        {
            FloatingIconManager.Instance.DestroySafely(gameObject);
        }
    }

    public void DisableObject(bool state)
    {
        if (state == false)
        {
            if (GetComponent<Rigidbody>())
            {
                GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
                GetComponent<Rigidbody>().useGravity = false;
                GetComponent<Rigidbody>().isKinematic = true;
            }

            GetComponent<MeshRenderer>().enabled = false;
            GetComponent<Collider>().enabled = false;

            if (transform.childCount > 0)
            {
                foreach (Transform child in transform.transform)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }
    }

    public void EnableObject()
    {
        if (GetComponent<Rigidbody>())
        {
            GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
            GetComponent<Rigidbody>().useGravity = true;
            GetComponent<Rigidbody>().isKinematic = false;
        }

        GetComponent<MeshRenderer>().enabled = true;
        GetComponent<Collider>().enabled = true;

        if (ItemType == Type.InventoryItem)
        {
            if (transform.childCount > 0)
            {
                foreach (Transform child in transform.transform)
                {
                    child.gameObject.SetActive(true);
                }
            }
        }
    }

    public Dictionary<string, object> OnSave()
    {
        if (GetComponent<MeshRenderer>())
        {
            return new Dictionary<string, object>()
            {
                { "position", transform.position },
                { "rotation", transform.eulerAngles },
                { "inv_id", InventoryID },
                { "inv_amount", Amount },
                { "weapon_id", WeaponID },
                { "customData", customData },
                { "isDisabled", GetComponent<MeshRenderer>().enabled }
            };
        }

        return null;
    }

    public void OnLoad(JToken token)
    {
        transform.position = token["position"].ToObject<Vector3>();
        transform.eulerAngles = token["rotation"].ToObject<Vector3>();
        InventoryID = (int)token["inv_id"];
        Amount = (int)token["inv_amount"];
        WeaponID = (int)token["weapon_id"];
        customData = token["customData"].ToObject<CustomItemData>();
        DisableObject(token["isDisabled"].ToObject<bool>());
    }
}

[System.Serializable]
public class ItemHashtable
{
    public string Key;
    public string Value;

    public ItemHashtable(string key, string value)
    {
        Key = key;
        Value = value;
    }
}