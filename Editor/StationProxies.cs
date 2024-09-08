#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.Constraint.Components;

namespace Myy
{

    public class ProxiedStation
    {
        public GameObject proxy;
        public GameObject stationObjectOnWorldLockedItem;
        public string pathWithParent;
        public bool setupDone;

        public ProxiedStation(VRCStation stationToProxy)
        {
            stationObjectOnWorldLockedItem = stationToProxy.gameObject;
            proxy = null;
            pathWithParent = "";
            setupDone = false;
        }

        public bool SetupProxy(GameObject proxyParent)
        {
            /* Add a proxy object to the Station Proxies list */ 
            GameObject newProxy = UnityEngine.Object.Instantiate(stationObjectOnWorldLockedItem, proxyParent.transform);

            /* Ensure the name is unique. If we have two "chairs" (Chair1 and Chair2) with both a child named 'Seat',
             * make sure we have Seat-01234 and Seat-56789 for example */
            newProxy.name += $"-{newProxy.GetInstanceID()}";

            /* We remove the VRCStation from the object fixed on the avatar, since we're going to use a Proxy instead */
            if (stationObjectOnWorldLockedItem.RemoveComponents<VRCStation>() == false)
            {
                MyyLogger.LogWarning($"In the end, no stations were found on object {stationObjectOnWorldLockedItem.name}");
                return false;
            }

            /* We'll assume that the colliders were setup for the station.
             * Remove them since we're removing the Station. */
            stationObjectOnWorldLockedItem.RemoveComponents<Collider>();

            /* Actually, VRChat generates an automatic collider if required */
            /*
            if (originalStationObject.RemoveComponents<Collider>() == false)
            {
                MyyLogger.Log(
                    $"Adding a collider to {originalStationObject.name} proxy, " +
                    "since none were found on the original");
                var collider = newProxy.AddComponent<BoxCollider>();
                collider.isTrigger = true;
            }*/

            /* Zero the position and rotation,
             * since the proxy will be fixed to the World Locked item using a Parent Constraint */

            /* We'll just use Scale Constraint for the Scale, so no need to change it.
             * Also the proxy won't have any renderer, so we don't have to worry about bouding box issues. */
            newProxy.transform.localPosition = Vector3.zero;
            newProxy.transform.localRotation = Quaternion.identity;

            /* Remove the children we might have copy from the fixed item */
            newProxy.RemoveChildren();

            /* Remove all the components beside VRCStation and Collider.
             * Note that we duplicated the fixed item before removing the Stations and Colliders from it.
             * So but we didn't remove them from the copy we made as a Proxy.
             */
            newProxy.KeepOnlyComponents(typeof(VRCStation), typeof(Collider));

            /* Disable the station 'Component' by default. It will then be enabled along with the fixed item. */ 
            newProxy.GetComponent<VRCStation>().enabled = false;
            foreach (var collider in newProxy.GetComponents<Collider>())
            {
                collider.enabled = false;
            }

            var mainSource = new VRCConstraintSource(stationObjectOnWorldLockedItem.transform, 1.0f, Vector3.zero, Vector3.zero);
            var parentConstraint = newProxy.AddComponent<VRCParentConstraint>();
            parentConstraint.Sources.Add(mainSource);
            parentConstraint.Locked = true;
            parentConstraint.IsActive = true;
            parentConstraint.enabled = false;

            var scaleConstraint = newProxy.AddComponent<VRCScaleConstraint>();
            scaleConstraint.Sources.Add(mainSource);
            scaleConstraint.Locked = true;
            scaleConstraint.IsActive = true;
            scaleConstraint.enabled = false;

            newProxy.SetActive(true);

            this.proxy = newProxy;
            this.pathWithParent = $"{proxyParent.name}/{newProxy.name}";
            this.setupDone = true;

            return true;
        }

    }

    public class ProxiedStations : List<ProxiedStation>
    {
        public bool SetupProxies(GameObject proxiesParent)
        {
            bool ret = true;
            foreach (var station in this)
            {
                bool stationSetup = station.SetupProxy(proxiesParent);
                if (!stationSetup)
                {
                    MyyLogger.LogWarning(
                        $"Could not setup station proxy for {station.stationObjectOnWorldLockedItem.name}");
                }
                ret &= stationSetup;
            }
            return ret;
        }

        public void PrepareFor(GameObject gameObject)
        {
            if (gameObject.TryGetComponent<VRCStation>(out VRCStation station))
            {
                Add(new ProxiedStation(station));
            }
        }

        /* TODO Why not add the curves to the AnimationClip directly ? */
        public void AddCurvesTo(AnimationClip notWorldLocked, AnimationClip worldLocked)
        {

            foreach (var proxiedStation in this)
            {
                if (!proxiedStation.setupDone)
                {
                    continue;
                }





                List<System.Type> toggledComponentTypes = new List<System.Type>(new System.Type[]
                {
                        typeof(VRCStation),
                        typeof(VRCParentConstraint),
                        typeof(VRCScaleConstraint),
                });

                /* Add the potential colliders on the station object, if there are any.
                 * If there are none, VRChat will add one automatically as long as the object
                 * is visible.
                 */
                {   
                    /* Take the first one. Won't bother if there's many colliders. */
                    var colliders = proxiedStation.proxy.GetComponents<UnityEngine.Collider>();
                    var collidersTypes = new HashSet<System.Type>(colliders.Length);

                    if (colliders != null && colliders.Length > 0)
                    {
                        foreach (var collider in colliders)
                        {
                            collidersTypes.Add(collider.GetType());
                        }
                    }

                    toggledComponentTypes.AddRange(collidersTypes);

                }

                string path = proxiedStation.pathWithParent;
                foreach (var componentType in toggledComponentTypes)
                {
                    notWorldLocked.SetCurve(path, componentType, "m_Enabled", false);
                    worldLocked.SetCurve(path, componentType, "m_Enabled", true);
                }


            }
        }
    }
}
#endif