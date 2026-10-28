using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ClawbearGames
{
    public class PoolManager : MonoBehaviour
    {
        public static PoolManager Instance { private set; get; }

        [SerializeField] private CubeController cubeControllerPrefab = null;
        [SerializeField] private PotController potControllerPrefab = null;
        [SerializeField] private CellController cellControllerPrefab = null;
        [SerializeField] private SawController sawControllerPrefab = null;
        [SerializeField] private HammerController hammerControllerPrefab = null;
        [SerializeField] private KeyController keyControllerPrefab = null;
        [SerializeField] private CoolerController coolerControllerPrefab = null;
        [SerializeField] private MagnetController magnetControllerPrefab = null;

        private List<CubeController> listCubeController = new List<CubeController>();
        private List<PotController> listPotController = new List<PotController>();
        private List<CellController> listCellController = new List<CellController>();
        private List<SawController> listSawController = new List<SawController>();
        private List<HammerController> listHammerController = new List<HammerController>();
        private List<KeyController> listKeyController = new List<KeyController>();
        private List<CoolerController> listCoolerController = new List<CoolerController>();
        private List<MagnetController> listMagnetController = new List<MagnetController>();


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
        /// Get an inactive CubeController object.
        /// </summary>
        /// <returns></returns>
        public CubeController GetCubeController()
        {
            //Find in the list
            CubeController cubeController = listCubeController.Where(a => !a.gameObject.activeSelf).FirstOrDefault();

            if (cubeController == null)
            {
                //Didn't find one -> create new one
                cubeController = Instantiate(cubeControllerPrefab, Vector3.zero, Quaternion.identity);
                cubeController.gameObject.SetActive(false);
                listCubeController.Add(cubeController);
            }

            return cubeController;
        }



        /// <summary>
        /// Get an inactive PotController object.
        /// </summary>
        /// <returns></returns>
        public PotController GetPotController()
        {
            //Find in the list
            PotController potController = listPotController.Where(a => !a.gameObject.activeSelf).FirstOrDefault();

            if (potController == null)
            {
                //Didn't find one -> create new one
                potController = Instantiate(potControllerPrefab, Vector3.zero, Quaternion.identity);
                potController.gameObject.SetActive(false);
                listPotController.Add(potController);
            }

            return potController;
        }



        /// <summary>
        /// Get an inactive CellController object.
        /// </summary>
        /// <returns></returns>
        public CellController GetCellController()
        {
            //Find in the list
            CellController cellController = listCellController.Where(a => !a.gameObject.activeSelf).FirstOrDefault();

            if (cellController == null)
            {
                //Didn't find one -> create new one
                cellController = Instantiate(cellControllerPrefab, Vector3.zero, Quaternion.identity);
                cellController.gameObject.SetActive(false);
                listCellController.Add(cellController);
            }

            return cellController;
        }


        /// <summary>
        /// Get an inactive SawController object.
        /// </summary>
        /// <returns></returns>
        public SawController GetSawController()
        {
            //Find in the list
            SawController sawController = listSawController.Where(a => !a.gameObject.activeSelf).FirstOrDefault();

            if (sawController == null)
            {
                //Didn't find one -> create new one
                sawController = Instantiate(sawControllerPrefab, Vector3.zero, Quaternion.identity);
                sawController.gameObject.SetActive(false);
                listSawController.Add(sawController);
            }

            return sawController;
        }



        /// <summary>
        /// Get an inactive HammerController object.
        /// </summary>
        /// <returns></returns>
        public HammerController GetHammerController()
        {
            //Find in the list
            HammerController hammerController = listHammerController.Where(a => !a.gameObject.activeSelf).FirstOrDefault();

            if (hammerController == null)
            {
                //Didn't find one -> create new one
                hammerController = Instantiate(hammerControllerPrefab, Vector3.zero, Quaternion.identity);
                hammerController.gameObject.SetActive(false);
                listHammerController.Add(hammerController);
            }

            return hammerController;
        }


        /// <summary>
        /// Get an inactive KeyController object.
        /// </summary>
        /// <returns></returns>
        public KeyController GetKeyController()
        {
            //Find in the list
            KeyController keyController = listKeyController.Where(a => !a.gameObject.activeSelf).FirstOrDefault();

            if (keyController == null)
            {
                //Didn't find one -> create new one
                keyController = Instantiate(keyControllerPrefab, Vector3.zero, Quaternion.identity);
                keyController.gameObject.SetActive(false);
                listKeyController.Add(keyController);
            }

            return keyController;
        }


        /// <summary>
        /// Get an inactive CoolerController object.
        /// </summary>
        /// <returns></returns>
        public CoolerController GetCoolerController()
        {
            //Find in the list
            CoolerController coolerController = listCoolerController.Where(a => !a.gameObject.activeSelf).FirstOrDefault();

            if (coolerController == null)
            {
                //Didn't find one -> create new one
                coolerController = Instantiate(coolerControllerPrefab, Vector3.zero, Quaternion.identity);
                coolerController.gameObject.SetActive(false);
                listCoolerController.Add(coolerController);
            }

            return coolerController;
        }


        /// <summary>
        /// Get an inactive CoolerController object.
        /// </summary>
        /// <returns></returns>
        public MagnetController GetMagnetController()
        {
            //Find in the list
            MagnetController magnetController = listMagnetController.Where(a => !a.gameObject.activeSelf).FirstOrDefault();

            if (magnetController == null)
            {
                //Didn't find one -> create new one
                magnetController = Instantiate(magnetControllerPrefab, Vector3.zero, Quaternion.identity);
                magnetController.gameObject.SetActive(false);
                listMagnetController.Add(magnetController);
            }

            return magnetController;
        }



    }
}
