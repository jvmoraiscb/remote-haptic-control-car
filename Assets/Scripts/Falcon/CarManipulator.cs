using UnityEngine;
using System.Collections;

public class CarManipulator : MonoBehaviour {

	public int falcon_num = 0;
	public bool[] button_states = new bool[4];
	bool [] curr_buttons = new bool[4];
	public Vector3 constantforce;

	private float minDistToMaxForce = 0.0005f;
	private float maxDistToMaxForce = 0.009f;
	public float hapticTipToWorldScale;
	
	private float savedHapticTipToWorldScale;
	
	public bool useMotionCompensator;
	
	private bool haveReceivedTipPosition = false;
	private int receivedCount = 0;

	private float minX = 10f;
	private float maxX = -10f;
	private float minY = 10f;
	private float maxY = -10f;
	private float minZ = 10f;
	private float maxZ = -10f;

	private float posX;
	private float posY;
	private float posZ;

	private const string HORIZONTAL = "Horizontal";
	private const string VERTICAL = "Vertical";

	private float horizontalInput;
	private float verticalInput;
	private float currentSteerAngle;
	private float currentbreakForce;
	private bool isBreaking = true;

	[SerializeField] private Transform carModel;
	[SerializeField] private Transform unityColliders;

	[SerializeField] private int rigidBodyId;

	[SerializeField] private Transform car;

	[SerializeField] private float motorForce;
	[SerializeField] private float breakForce;
	[SerializeField] private float maxSteerAngle;

	[SerializeField] private Transform frontLeftWheelTransform;
	[SerializeField] private Transform frontRightWheeTransform;
	[SerializeField] private Transform rearLeftWheelTransform;
	[SerializeField] private Transform rearRightWheelTransform;

	[SerializeField] private WheelCollider frontLeftWheelCollider;
	[SerializeField] private WheelCollider frontRightWheelCollider;
	[SerializeField] private WheelCollider rearLeftWheelCollider;
	[SerializeField] private WheelCollider rearRightWheelCollider;
	
	// Use this for initialization
	void Start () {
		
		
		savedHapticTipToWorldScale = hapticTipToWorldScale;
		
		FalconUnity.setForceField(falcon_num,constantforce);
		
		Vector3 tipPositionScale = new Vector3(1,1,-1);
		tipPositionScale *= hapticTipToWorldScale;
		
		FalconUnity.updateHapticTransform(falcon_num, transform.position, transform.rotation, tipPositionScale, useMotionCompensator, 1/60.0f);
			
	}
	
	// Update is called once per frame
	void Update () {
		
		if (! haveReceivedTipPosition ) {
			Vector3 posTip2;
			bool result = FalconUnity.getTipPosition(falcon_num, out posTip2);
			if(!result){
//				Debug.Log("Error getting tip position");
				return;
			}
			receivedCount ++;
			
			if (receivedCount < 25 && (posTip2.x == 0 && posTip2.y == 0 &&posTip2.z == 0)) {
				return;
			}
			
			Debug.Log ("Initialized with tip position: ");
			Debug.Log (posTip2);
			FalconUnity.setRigidBodyGodObject(falcon_num, rigidBodyId, minDistToMaxForce * hapticTipToWorldScale, maxDistToMaxForce * hapticTipToWorldScale);
			haveReceivedTipPosition = true;
		}
		
		Vector3 tipPositionScale = new Vector3(1,1,-1);
		tipPositionScale *= hapticTipToWorldScale;
		
		if (savedHapticTipToWorldScale != hapticTipToWorldScale) {
			FalconUnity.setRigidBodyGodObject(falcon_num, rigidBodyId, minDistToMaxForce * hapticTipToWorldScale, maxDistToMaxForce * hapticTipToWorldScale);
			savedHapticTipToWorldScale = hapticTipToWorldScale;
			
		}
			
		FalconUnity.updateHapticTransform(falcon_num, transform.position, transform.rotation, tipPositionScale, useMotionCompensator, Time.deltaTime);
		
		Vector3 posGod;
		bool res = FalconUnity.getGodPosition(falcon_num, out posGod);
		if(!res){
//			Debug.Log("Error getting god tip position");
			return;
		}
		Vector3 posTip;
		res = FalconUnity.getTipPosition(falcon_num, out posTip);
		if(!res){
//			Debug.Log("Error getting tip position");
			return;
		}

		float tempX = posTip.x;
		float tempY = posTip.y;
		float tempZ = posTip.z;

		if (tempX > maxX) {
        maxX = tempX;
    }
		if (tempY > maxY) {
        maxY = tempY;
    }
		if (tempZ > maxZ) {
        maxZ = tempZ;
    }

		if (tempX < minX) {
        minX = tempX;
    }
		if (tempY < minY) {
        minY = tempY;
    }
		if (tempZ < minZ) {
        minZ = tempZ;
    }

		posX = ((tempX - minX) / (maxX - minX)) * 2 - 1;
		posY = ((tempY - minY) / (maxY - minY)) * 2 - 1;
		posZ = ((tempZ - minZ) / (maxZ - minZ)) * 2 - 1;

		Debug.Log("x:" + posX + " y:" + posY + " z:" + posZ);
		//	godObject.rotation = new Quaternion(0,0,0,1);
		//	FalconUnity.setForceField(falcon_num,force);
				 
	}

	    private void FixedUpdate()
    {
					GetInput();
        	HandleMotor();
        	HandleSteering();
					UpdateWheels();
					carModel.position = unityColliders.position;
					carModel.rotation = unityColliders.rotation;
    }


    private void GetInput()
    {
        horizontalInput = posX;
        verticalInput = posZ;

    }

    private void HandleMotor()
    {
        frontLeftWheelCollider.motorTorque = verticalInput * motorForce;
        frontRightWheelCollider.motorTorque = verticalInput * motorForce;
        currentbreakForce = isBreaking ? breakForce : 0f;
        ApplyBreaking();
    }

    private void ApplyBreaking()
    {
        frontRightWheelCollider.brakeTorque = currentbreakForce;
        frontLeftWheelCollider.brakeTorque = currentbreakForce;
        rearLeftWheelCollider.brakeTorque = currentbreakForce;
        rearRightWheelCollider.brakeTorque = currentbreakForce;
    }

    private void HandleSteering()
    {
        currentSteerAngle = maxSteerAngle * horizontalInput;
        frontLeftWheelCollider.steerAngle = currentSteerAngle;
        frontRightWheelCollider.steerAngle = currentSteerAngle;
    }

		private void UpdateWheels()
    {
        UpdateSingleWheel(frontLeftWheelCollider, frontLeftWheelTransform);
        UpdateSingleWheel(frontRightWheelCollider, frontRightWheeTransform);
        UpdateSingleWheel(rearRightWheelCollider, rearRightWheelTransform);
        UpdateSingleWheel(rearLeftWheelCollider, rearLeftWheelTransform);
    }

    private void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform)
    {
        Vector3 pos;
        Quaternion rot; 
        wheelCollider.GetWorldPose(out pos, out rot);
        wheelTransform.rotation = rot;
        wheelTransform.position = pos;
    }
	
	
	void LateUpdate() {
	
		bool res = FalconUnity.getFalconButtonStates(falcon_num, out curr_buttons);

		
		if(!res){
//			Debug.Log("Error getting button states");
			return;
		}
		//go through the buttons, seeing which are pressed
		for(int i=0;i<4;i++){
			if(button_states[i] && button_states[i] != curr_buttons[i]){
				buttonPressed(i);
			}
			else if(button_states[i] && button_states[i] != curr_buttons[i]){
				buttonReleased(i);
			}
			button_states[i] = curr_buttons[i];
		}
	}
	
	
	void buttonPressed(int i){
		
		switch(i){
		case 0:
		if(isBreaking){
			isBreaking = false;
		}			
		else{
			isBreaking = true;
		}
			break;
		case 1: 
			break;
		case 2:
			
			break;
		case 3:
			break;
			
		}
	}
	void buttonReleased(int i){
		
		switch(i){
		case 0:
			break;
		case 1: 
			break;
		case 2:
			break;
		case 3:
			break;
			
		}
	}	
}
