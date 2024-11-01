import requests
import sys
import os
import json

def generate_3d_model(input_path, output_path, api_key):
    """
    Generate a 3D model using Stability AI's API
    
    Args:
        input_path (str): Path to the input image
        output_path (str): Directory where the output model will be saved
        api_key (str): Stability AI API key
    """
    try:
        # Validate input file exists
        if not os.path.exists(input_path):
            print(json.dumps({
                "success": False,
                "error": f"Input file not found: {input_path}"
            }))
            return

        # Validate output directory exists
        if not os.path.exists(output_path):
            print(json.dumps({
                "success": False,
                "error": f"Output directory not found: {output_path}"
            }))
            return

        # Make API request
        response = requests.post(
            f'https://api.stability.ai/v2beta/3d/stable-fast-3d',
            headers={
                'authorization': f'Bearer {api_key}',
            },
            files={
                'image': open(input_path, 'rb')
            },
            data={},
        )
        
        if response.status_code == 200:
            # Generate output filename based on input filename
            input_filename = os.path.splitext(os.path.basename(input_path))[0]
            output_file = os.path.join(output_path, f'{input_filename}_3d.glb')
            
            # Save the model
            with open(output_file, 'wb') as file:
                file.write(response.content)
            
            # Return success message as JSON
            print(json.dumps({
                "success": True,
                "message": f"3D model generated successfully",
                "output_path": output_file
            }))
        else:
            # Return error message as JSON
            print(json.dumps({
                "success": False,
                "error": f"API Error: {response.status_code}",
                "details": response.text
            }))
            
    except Exception as e:
        # Return any other errors as JSON
        print(json.dumps({
            "success": False,
            "error": str(e)
        }))

if __name__ == '__main__':
    if len(sys.argv) != 4:
        print(json.dumps({
            "success": False,
            "error": "Invalid arguments. Required: input_path output_path api_key"
        }))
    else:
        input_path = sys.argv[1]
        output_path = sys.argv[2]
        api_key = sys.argv[3]
        generate_3d_model(input_path, output_path, api_key)