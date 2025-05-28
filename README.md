This solution uses gRPC for the communication and LiteDB for  concurrent safe persistence. An initial JSON based implementation was replaced due to limitations under concurrent and parallel access.

Testing - To test the project, start the server and either:

    Run the console client to test manually

    Execute the unit tests for automated concurrency validation
