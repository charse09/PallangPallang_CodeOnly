using UnityEngine;

public class CharaBlendTree : MonoBehaviour
{
    private Animator anim;
    public float rotateSpeed = 25.0f;


    void Start()
    {
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        anim.SetFloat("Vertical", Input.GetAxis("Vertical"));
        if(Input.GetAxis("Vertical") != 0)
        {
            if (Input.GetAxis("Horizontal") > 0)
            {
                transform.Rotate(0, rotateSpeed * Time.deltaTime, 0);
            }
            if(Input.GetAxis("Horizontal") < 0)
            {
                transform.Rotate(0, -rotateSpeed * Time.deltaTime, 0);
            }
        }
    }
}
