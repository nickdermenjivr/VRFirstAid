using UnityEngine;

namespace Autohand{
    [RequireComponent(typeof(ConfigurableJoint))]
    public class PhysicsGadgetConfigurableLimitReader : MonoBehaviour{
        public bool invertValue = false;
        [Tooltip("For objects slightly off center. " +
            "\nThe minimum abs value required to return a value nonzero value\n " +
            "- if playRange is 0.1, you have to move the gadget 10% to get a result")]
        public float playRange = 0.025f;
        protected ConfigurableJoint joint;

        protected Vector3 axisPos;
        float value;
        Vector3 limitAxis;
        protected Vector3 angularAxisPos;

        protected virtual void Start(){
            joint = GetComponent<ConfigurableJoint>();
            limitAxis = new Vector3(joint.xMotion == ConfigurableJointMotion.Locked ? 0 : 1, joint.yMotion == ConfigurableJointMotion.Locked ? 0 : 1, joint.zMotion == ConfigurableJointMotion.Locked ? 0 : 1);
            axisPos = Vector3.Scale(transform.localPosition, limitAxis);

            angularAxisPos = transform.localEulerAngles;
        }


        /// <summary>Returns a -1 to 1 value that represents the point of the slider</summary>
        public float GetValue() {
            bool positive = true;
            var currPos = Vector3.Scale(transform.localPosition, limitAxis);
            if(axisPos.x < currPos.x || axisPos.y < currPos.y || axisPos.z < currPos.z)
                positive = false;

            if(invertValue)
                positive = !positive;

            value = Vector3.Distance(axisPos, currPos)/joint.linearLimit.limit;

            if(!positive) value *= -1;

            if (Mathf.Abs(value) < playRange)
                value = 0;
            return Mathf.Clamp(value, -1f, 1f);
        }
        
        public float GetAngleValue() {
            bool positive = true;
            var currPos = transform.localEulerAngles;
            if(angularAxisPos.x < currPos.x || angularAxisPos.y < currPos.y || angularAxisPos.z < currPos.z)
                positive = false;

            if(invertValue)
                positive = !positive;

            var minValue = joint.lowAngularXLimit.limit;
            var maxValue = joint.highAngularXLimit.limit;
            
            value = currPos.x > 180 ? currPos.x - angularAxisPos.x - 360 : currPos.x - angularAxisPos.x;
            
            if(!positive) value *= -1;
            
            var res = Mathf.Clamp(value, minValue, maxValue);
            
            if (Mathf.Abs(value) < playRange)
                res = 0;

            return Mathf.Clamp(res, -1f, 1f);
        }

        public ConfigurableJoint GetJoint() => joint;
    }
}
