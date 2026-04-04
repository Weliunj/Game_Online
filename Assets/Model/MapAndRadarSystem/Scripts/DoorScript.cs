using UnityEngine;

namespace MapAndRadarSystem
{
    [RequireComponent(typeof(AudioSource), typeof(Animator))]
    public class DoorScript : MonoBehaviour
    {
        private Animator animator;
        public AudioClip audioOpen;
        public AudioClip audioClose;
        private AudioSource audioSource;

        private const float interactionCooldown = 0.5f;
        private float lastTimeInteracted = -1f;

        void Awake()
        {
            animator = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();
        }

        public void DoorInteraction(bool open)
        {
            if (Time.time > lastTimeInteracted + interactionCooldown)
            {
                lastTimeInteracted = Time.time;
                if (open)
                {
                    animator.SetTrigger("Open");
                    if (audioOpen != null)
                        audioSource.PlayOneShot(audioOpen);
                }
                else
                {
                    animator.SetTrigger("Close");
                    if (audioClose != null)
                        audioSource.PlayOneShot(audioClose);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.CompareTag("NPC"))
            {
                DoorInteraction(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") || other.CompareTag("NPC"))
            {
                DoorInteraction(false);
            }
        }
    }
}
