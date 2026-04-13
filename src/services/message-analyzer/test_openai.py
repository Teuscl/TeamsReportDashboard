import asyncio
from analysis_logic import client

async def test():
    try:
        models = await client.models.list()
        print("Success:", len(models.data))
    except Exception as e:
        print("Error:", type(e), e)

asyncio.run(test())
