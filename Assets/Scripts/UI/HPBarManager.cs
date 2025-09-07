using UnityEngine;
using UnityEngine.UI;

namespace Microlight.MicroBar {
    public class HPBarManager : MonoBehaviour {
        [Header("Prefabs")]
     [SerializeField] MicroBar HealthBar_UI;
        [SerializeField] MicroBar HealthBar_Char;

        [Header("Health Bar Holders")]
      //  [SerializeField] Transform leftBarHolder;

        [SerializeField] Transform UIBarHolder;
        [SerializeField] public Transform mycharBarHolder;
        [SerializeField] public Transform opcharBarHolder;
        //      
        //        [SerializeField] Transform rightBarHolder;

      

        [Header("Sounds")]
      //  [SerializeField] AudioClip hurtSound;
     //   [SerializeField] AudioClip healSound;
      //  [SerializeField] AudioSource soundSource;
     //   [SerializeField] Text soundButtonText;
        bool soundOn = false;

        
        MicroBar UIMicroBar;
        MicroBar myCharMicroBar;
        MicroBar opCharMicroBar;


        int myHP;
        int opHP;

       
        public static HPBarManager Instance { get; private set; }

        // ... (기존 필드들은 그대로)

        private void Awake()
        {
            // 싱글톤 보장
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

         
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

       
        //  private void Start() {
        //    CurrentType = 1;
        //}

        public void HPBar_Init(int StartingHp)
        {
            UIMicroBar = Instantiate(HealthBar_UI, UIBarHolder); ;
            myCharMicroBar = Instantiate(HealthBar_Char, mycharBarHolder); ;
            opCharMicroBar = Instantiate(HealthBar_Char, opcharBarHolder); ;



            if (UIMicroBar != null) UIMicroBar.Initialize(StartingHp);

            if (myCharMicroBar != null) myCharMicroBar.Initialize(StartingHp);

            if (opCharMicroBar != null) opCharMicroBar.Initialize(StartingHp);

            myHP = StartingHp;
            opHP = StartingHp;
        }

        #region Damage/Heal



        public void DamageMe(int newHP)
        {
            float damageAmount = myHP- newHP;
            if (damageAmount < 0)
                return;


            // Update HealthBar
            if (UIMicroBar != null) UIMicroBar.UpdateBar(newHP);
            if (myCharMicroBar != null) {
                myCharMicroBar.UpdateBar(newHP);
                
            }
            myHP = newHP;

            //leftAnimator.SetTrigger("Damage");
        }
        public void DamageOp(int  newHP)
        {
            float damageAmount = opHP - newHP;
            if (damageAmount < 0)
                return;

            //  soundSource.clip = hurtSound;
            // if(soundOn) soundSource.Play();

            // Update HealthBar
            if (opCharMicroBar != null) 
            {
                opCharMicroBar.UpdateBar(newHP);
                Debug.Log("된겨 안된겨");
            }
            opHP = newHP;
            //leftAnimator.SetTrigger("Damage");
        }
   
        /*public void HealLeft() {
            float healAmount = Random.Range(5f, 15f);
            hpLeft += healAmount;
            if(hpLeft > MAX_HP) hpLeft = MAX_HP;
        //    soundSource.clip = healSound;
        //    if(soundOn) soundSource.Play();

            // Update HealthBar
            if(leftMicroBar != null) leftMicroBar.UpdateBar(hpLeft, false, UpdateAnim.Heal);
       //     leftAnimator.SetTrigger("Heal");
        }
      */
        #endregion

           
     

        #region Sound
        public void ToggleSound() {
            soundOn = !soundOn;
          //  if(soundOn) soundButtonText.text = "On";
        //    else soundButtonText.text = "Off";
        }
        #endregion
    }
}