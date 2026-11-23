using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private float speed;

        private void Start()
        {
            transform.position = new Vector3(-5f, Random.Range(-5f, 5f), 0f);
        }

        void Update()
        {
            transform.position += new Vector3(Keyboard.current.dKey.isPressed ? speed : 0f, 0f) * Time.deltaTime;
        }
    }
}
