using UnityEngine;

public interface INPCReaction
{
    void HitReaction();
    void SoundReaction(int type, float distance, Vector3 pos);
}
