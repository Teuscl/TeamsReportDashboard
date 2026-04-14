from openai import AsyncOpenAI
import asyncio

async def test():
    try:
        client = AsyncOpenAI(api_key=None)
        await client.models.list()
    except Exception as e:
        print("Error details:", type(e), e)

asyncio.run(test())
