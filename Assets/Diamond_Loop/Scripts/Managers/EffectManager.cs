using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace ClawbearGames
{
    public class EffectManager : MonoBehaviour
    {

        public static EffectManager Instance { private set; get; }

        [SerializeField] private ParticleSystem potDeployEffectPrefab = null;
        [SerializeField] private ParticleSystem potCompletedEffectPrefab = null;
        [SerializeField] private ParticleSystem potDecayedEffectPrefab = null;
        [SerializeField] private ParticleSystem frozenBreakEffectPrefab = null;
        [SerializeField] private ParticleSystem lockedBreakEffectPrefab = null;
        [SerializeField] private ParticleSystem lockedUnlockEffectPrefab = null;
        [SerializeField] private ParticleSystem decayCoolOffEffectPrefab = null;



        private List<ParticleSystem> listPotDeployEffect = new List<ParticleSystem>();
        private List<ParticleSystem> listPotCompletedEffect = new List<ParticleSystem>();
        private List<ParticleSystem> listPotDecayedEffect = new List<ParticleSystem>();
        private List<ParticleSystem> listFrozenBreakEffect = new List<ParticleSystem>();
        private List<ParticleSystem> listLockedBreakEffect = new List<ParticleSystem>();
        private List<ParticleSystem> listLockedUnlockEffect = new List<ParticleSystem>();
        private List<ParticleSystem> listDecayCoolOffEffect = new List<ParticleSystem>();


        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                DestroyImmediate(Instance.gameObject);
                Instance = this;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }


        /// <summary>
        /// Play the given particle then disable it 
        /// </summary>
        /// <param name="par"></param>
        /// <returns></returns>
        private IEnumerator CRPlayParticle(ParticleSystem par)
        {
            par.Play();
            yield return new WaitForSeconds(2f);
            par.gameObject.SetActive(false);
            par.transform.SetParent(null);
        }



        /// <summary>
        /// Play a pot deploy effect at given position.
        /// </summary>
        /// <param name="pot"></param>
        public void PlayPotDeployEffect(Transform pot)
        {
            //Find in the list
            ParticleSystem potDeployEffect = listPotDeployEffect.Where(a => !a.gameObject.activeSelf).FirstOrDefault();

            if (potDeployEffect == null)
            {
                //Didn't find one -> create new one
                potDeployEffect = Instantiate(potDeployEffectPrefab, pot.transform.position, Quaternion.identity);
                potDeployEffect.gameObject.SetActive(false);
                listPotDeployEffect.Add(potDeployEffect);
            }

            potDeployEffect.transform.SetParent(pot);
            potDeployEffect.transform.localPosition = Vector3.zero;
            potDeployEffect.gameObject.SetActive(true);
            StartCoroutine(CRPlayParticle(potDeployEffect));
        }



        /// <summary>
        /// Play a frozen break effect at given position.
        /// </summary>
        /// <param name="pos"></param>
        public void PlayFrozenBreakEffect(Vector3 pos)
        {
            //Find in the list
            ParticleSystem frozenBreakEffect = listFrozenBreakEffect.Where(a => !a.gameObject.activeSelf).FirstOrDefault();

            if (frozenBreakEffect == null)
            {
                //Didn't find one -> create new one
                frozenBreakEffect = Instantiate(frozenBreakEffectPrefab, pos, Quaternion.identity);
                frozenBreakEffect.gameObject.SetActive(false);
                listFrozenBreakEffect.Add(frozenBreakEffect);
            }

            frozenBreakEffect.transform.position = pos + Vector3.up * 0.01f;
            frozenBreakEffect.gameObject.SetActive(true);
            StartCoroutine(CRPlayParticle(frozenBreakEffect));
        }



        /// <summary>
        /// Play a pot decayed effect at given position.
        /// </summary>
        /// <param name="pos"></param>
        public void PlayPotDecayedEffect(Vector3 pos)
        {
            //Find in the list
            ParticleSystem potDecayedEffect = listPotDecayedEffect.Where(a => !a.gameObject.activeSelf).FirstOrDefault();

            if (potDecayedEffect == null)
            {
                //Didn't find one -> create new one
                potDecayedEffect = Instantiate(potDecayedEffectPrefab, pos, Quaternion.identity);
                potDecayedEffect.gameObject.SetActive(false);
                listPotDecayedEffect.Add(potDecayedEffect);
            }

            potDecayedEffect.transform.position = pos + Vector3.up * 0.01f;
            potDecayedEffect.gameObject.SetActive(true);
            StartCoroutine(CRPlayParticle(potDecayedEffect));
        }



        /// <summary>
        /// Play a decay cool off effect at given position.
        /// </summary>
        /// <param name="pos"></param>
        public void PlayDecayCoolOffEffect(Vector3 pos)
        {
            //Find in the list
            ParticleSystem decayCoolOffEffect = listDecayCoolOffEffect.Where(a => !a.gameObject.activeSelf).FirstOrDefault();

            if (decayCoolOffEffect == null)
            {
                //Didn't find one -> create new one
                decayCoolOffEffect = Instantiate(decayCoolOffEffectPrefab, pos, Quaternion.identity);
                decayCoolOffEffect.gameObject.SetActive(false);
                listDecayCoolOffEffect.Add(decayCoolOffEffect);
            }

            decayCoolOffEffect.transform.position = pos + Vector3.up * 0.01f;
            decayCoolOffEffect.gameObject.SetActive(true);
            StartCoroutine(CRPlayParticle(decayCoolOffEffect));
        }


        /// <summary>
        /// Play a rocket explode effect at given position.
        /// </summary>
        /// <param name="pos"></param>
        public void PlayPotCompletedEffect(Vector3 pos)
        {
            //Find in the list
            ParticleSystem potCompletedEffect = listPotCompletedEffect.Where(a => !a.gameObject.activeSelf).FirstOrDefault();

            if (potCompletedEffect == null)
            {
                //Didn't find one -> create new one
                potCompletedEffect = Instantiate(potCompletedEffectPrefab, pos, Quaternion.identity);
                potCompletedEffect.gameObject.SetActive(false);
                listPotCompletedEffect.Add(potCompletedEffect);
            }

            potCompletedEffect.transform.position = pos;
            potCompletedEffect.gameObject.SetActive(true);
            StartCoroutine(CRPlayParticle(potCompletedEffect));
        }



        /// <summary>
        /// Play a locked break effect at given position.
        /// </summary>
        /// <param name="pos"></param>
        public void PlayLockedBreakExplodeEffect(Vector3 pos)
        {
            //Find in the list
            ParticleSystem lockedBreakEffect = listLockedBreakEffect.Where(a => !a.gameObject.activeSelf).FirstOrDefault();

            if (lockedBreakEffect == null)
            {
                //Didn't find one -> create new one
                lockedBreakEffect = Instantiate(lockedBreakEffectPrefab, pos, Quaternion.identity);
                lockedBreakEffect.gameObject.SetActive(false);
                listLockedBreakEffect.Add(lockedBreakEffect);
            }

            lockedBreakEffect.transform.position = pos + Vector3.up * 0.01f;
            lockedBreakEffect.gameObject.SetActive(true);
            StartCoroutine(CRPlayParticle(lockedBreakEffect));
        }


        /// <summary>
        /// Play a locked unlock effect at given position.
        /// </summary>
        /// <param name="pos"></param>
        public void PlayLockedUnlockEffect(Vector3 pos)
        {
            //Find in the list
            ParticleSystem lockedUnlockEffect = listLockedUnlockEffect.Where(a => !a.gameObject.activeSelf).FirstOrDefault();

            if (lockedUnlockEffect == null)
            {
                //Didn't find one -> create new one
                lockedUnlockEffect = Instantiate(lockedUnlockEffectPrefab, pos, Quaternion.identity);
                lockedUnlockEffect.gameObject.SetActive(false);
                listLockedUnlockEffect.Add(lockedUnlockEffect);
            }

            lockedUnlockEffect.transform.position = pos + Vector3.up * 0.01f;
            lockedUnlockEffect.gameObject.SetActive(true);
            StartCoroutine(CRPlayParticle(lockedUnlockEffect));
        }


    }
}