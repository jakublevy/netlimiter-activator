import uvicorn
from fastapi import FastAPI

app = FastAPI()


@app.post("/api/pub/CheckLicense")
def checkLicense(payload: dict):
    apiResponse = {
        'success': True,
        'message': '',
        'details': '',
        'statusCode': 0,
        'result': {
            'regData': {
                'version': 1,
                'productId': 'nl',
                'editionId': 'pro',
                'licenseId': 'standard',
                'planId': 'two-years',
                'isRecurring': False,
                'isCancelled': False,
                'endTime': '2040-04-04T04:00:00Z',
                'quantity': 1,
                'regName': 'Unlimited License',
                'regCodeHash': '',
                'hWCodeHash': '',
                'signature': ''
            }
        }
    }
    return apiResponse


if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=80)
